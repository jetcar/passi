using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using ConfigurationManager;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Models;
using NUnit.Framework;
using passi_webapi.SmartHome;
using Repos;
using WebApiDto.SmartHome;

namespace PassiWebApiTests;

// Real PostgreSQL transactions, JWT signature validation, HTTP authorization and SignalR framing.
// TestServer substitutes only the network transport; no physical-device endpoint is ever called.
[TestFixture, NonParallelizable]
public class SmartHomeTransportTests
{
    private const string Api = "/passiapi/api/smarthome";
    private const string Issuer = "https://identity.example/openidc";
    private const string Audience = "PassiWebApp";
    private IContainer postgres;
    private TestServer server;
    private HttpClient client;
    private RSA rsa;
    private RsaSecurityKey key;
    private int testPeer;
    private IPAddress proxyPeer;
    private readonly MutableClock clock = new();
    private readonly List<HubConnection> connections = new();

    private sealed class MutableClock : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = DateTimeOffset.UtcNow;
        public override DateTimeOffset GetUtcNow() => Now;
        public void Advance(int seconds) => Now = Now.AddSeconds(seconds);
    }

    [OneTimeSetUp]
    public async Task Start()
    {
        var databasePassword = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        var builder = new ContainerBuilder().WithImage("postgres:15.5")
            .WithPortBinding(5432, true).WithEnvironment("POSTGRES_PASSWORD", databasePassword)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilMessageIsLogged("database system is ready to accept connections"));
        var dockerEndpoint = Environment.GetEnvironmentVariable("DOCKER_HOST");
        if (!string.IsNullOrEmpty(dockerEndpoint)) builder = builder.WithDockerEndpoint(dockerEndpoint);
        postgres = builder.Build();
        await postgres.StartAsync();
        rsa = RSA.Create(2048);
        key = new RsaSecurityKey(rsa) { KeyId = "local-transport-test" };
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string>
        {
            ["AppSetting:IdentityUrlBase"] = "https://identity.example",
            ["AppSetting:ClientId"] = Audience,
            ["AppSetting:DbHost"] = postgres.Hostname,
            ["AppSetting:DbPort"] = postgres.GetMappedPublicPort(5432).ToString(),
            ["AppSetting:PassiDbName"] = "smarthome_transport",
            ["AppSetting:DbUser"] = "postgres",
            ["AppSetting:DbPassword"] = databasePassword,
            ["AppSetting:DbSslMode"] = "Disable",
            ["AppSetting:SmartHomeTrustedProxyAddresses"] = "192.0.2.10"
        }).Build();
        var settings = new AppSetting(config) { PrefferAppsettingFile = true };
        server = new TestServer(new WebHostBuilder().ConfigureServices(services =>
        {
            services.AddSingleton(settings);
            services.AddScoped<PassiDbContext>();
            services.AddScoped<CurrentContext>();
            services.AddControllers().AddApplicationPart(typeof(SmartHomeController).Assembly);
            services.AddSmartHome(settings);
            services.Replace(ServiceDescriptor.Singleton<TimeProvider>(clock));
            // Supply a test issuer's public key, retaining the production JWT validation and events.
            services.PostConfigure<JwtBearerOptions>(SmartHomeRegistration.OwnerScheme, options =>
            {
                var metadata = new OpenIdConnectConfiguration { Issuer = Issuer };
                metadata.SigningKeys.Add(key);
                options.ConfigurationManager = new StaticConfigurationManager<OpenIdConnectConfiguration>(metadata);
            });
        }).Configure(app =>
        {
            // Separate TestServer peers isolate the production per-IP enrollment rate limiter.
            app.Use((context, next) =>
            {
                context.Connection.RemoteIpAddress = proxyPeer ?? IPAddress.Parse($"127.0.0.{testPeer}");
                return next();
            });
            app.UseRouting();
            app.UseMiddleware<SmartHomeForwardingMiddleware>();
            app.UseAuthentication();
            app.UseRateLimiter();
            app.UseSmartHomeBoundary();
            app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapHub<HomeAgentHub>("/api/smarthome/agent/hub", options => options.Transports = HttpTransportType.WebSockets);
            });
        }));
        client = server.CreateClient();
        client.BaseAddress = new Uri("https://localhost");
        using var scope = server.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<PassiDbContext>().Database.EnsureCreatedAsync();
    }

    [SetUp]
    public async Task Seed()
    {
        testPeer++;
        proxyPeer = null;
        clock.Now = DateTimeOffset.UtcNow;
        using var scope = server.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PassiDbContext>();
        await db.SmartHomeCommands.ExecuteDeleteAsync();
        await db.SmartHomes.ExecuteDeleteAsync();
        await db.SmartHomeOwners.ExecuteDeleteAsync();
        await db.Certificates.ExecuteDeleteAsync();
        await db.Invitations.ExecuteDeleteAsync();
        await db.Users.ExecuteDeleteAsync();
        foreach (var email in new[] { "admin@passi.cloud", "other@example.test" })
        {
            var user = new UserDb { Guid = Guid.NewGuid(), EmailHash = email };
            user.Certificates.Add(new CertificateDb { Thumbprint = email + "-certificate" });
            user.Invitations.Add(new UserInvitationDb { IsConfirmed = true });
            db.Users.Add(user);
        }
        await db.SaveChangesAsync();
    }

    [TearDown]
    public async Task Disconnect()
    {
        foreach (var connection in connections) await connection.DisposeAsync();
        connections.Clear();
    }

    [OneTimeTearDown]
    public async Task Stop()
    {
        client?.Dispose();
        server?.Dispose();
        rsa?.Dispose();
        if (postgres != null) await postgres.DisposeAsync();
    }

    private string Jwt(string subject = "admin@passi.cloud", string audience = Audience,
        string clientId = Audience, bool accessToken = true, bool expired = false, SecurityKey signingKey = null,
        string thumbprint = null, string issuer = Issuer)
    {
        var claims = new List<Claim> { new("sub", subject), new("Thumbprint", thumbprint ?? subject + "-certificate"),
            new("email", "admin@passi.cloud"), new(SmartHomeRegistration.OwnerClaim, Guid.NewGuid().ToString()) };
        if (accessToken) { claims.Add(new("client_id", clientId)); claims.Add(new("scope", "openid profile")); }
        var now = DateTime.UtcNow;
        return new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(issuer, audience, claims,
            now.AddHours(-2), expired ? now.AddHours(-1) : now.AddHours(1),
            new SigningCredentials(signingKey ?? key, SecurityAlgorithms.RsaSha256)));
    }

    private Task<HttpResponseMessage> Send(HttpMethod method, string path, string token = null, object body = null,
        string forwardedProto = null, string forwardedFor = null)
    {
        var request = new HttpRequestMessage(method, path.Replace("/passiapi", ""));
        if (token != null) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (forwardedProto != null) request.Headers.Add("X-Forwarded-Proto", forwardedProto);
        if (forwardedFor != null) request.Headers.Add("X-Forwarded-For", forwardedFor);
        if (body != null) request.Content = JsonContent.Create(body);
        return client.SendAsync(request);
    }

    private async Task<T> Read<T>(HttpResponseMessage response)
    {
        Assert.That(response.IsSuccessStatusCode, Is.True, await response.Content.ReadAsStringAsync());
        return await response.Content.ReadFromJsonAsync<T>();
    }

    private async Task<EnrollmentDto> CreateHome() => await Read<EnrollmentDto>(await Send(HttpMethod.Post,
        Api + "/homes", Jwt(), new { name = "Emulated home" }));

    private async Task<AgentTokenDto> Enroll(EnrollmentDto enrollment) => await Read<AgentTokenDto>(await Send(HttpMethod.Post,
        Api + $"/agent/homes/{enrollment.HomeId}/enroll", body: new { enrollment.EnrollmentSecret }));

    private HubConnection NewAgent(string token, string query = "", bool forwardedHttp = false)
    {
        var scheme = forwardedHttp ? "http" : "https";
        var connection = new HubConnectionBuilder().WithUrl(scheme + "://localhost/api/smarthome/agent/hub" + query, options =>
        {
            options.Transports = HttpTransportType.WebSockets;
            options.SkipNegotiation = true;
            options.Headers["Authorization"] = "Bearer " + token;
            options.WebSocketFactory = async (context, cancellation) =>
            {
                var websocket = server.CreateWebSocketClient();
                websocket.ConfigureRequest = request =>
                {
                    request.Headers.Authorization = "Bearer " + token;
                    if (forwardedHttp) request.Headers["X-Forwarded-Proto"] = "https";
                };
                return await websocket.ConnectAsync(context.Uri, cancellation);
            };
        }).Build();
        connections.Add(connection);
        return connection;
    }

    private async Task<HubConnection> Connect(AgentTokenDto token)
    {
        var connection = NewAgent(token.Token);
        await connection.StartAsync();
        await connection.InvokeAsync<ExchangeResponse>("Exchange", Snapshot());
        return connection;
    }

    private ExchangeRequest Snapshot(bool power = false) => new()
    {
        SnapshotAt = clock.Now,
        Devices = new List<DeviceDto> { new() { Id = "xiaomi-plug", Name = "Emulated Xiaomi plug", Online = true,
            Capabilities = new[] { "power.read", "temperature.read", "power.set" }, Power = power,
            TemperatureCelsius = 26.5, UpdatedAt = clock.Now } }
    };

    private async Task<CommandDto> Queue(Guid home, Guid? id = null, bool on = true) => await Read<CommandDto>(await Send(
        HttpMethod.Post, Api + $"/homes/{home}/devices/xiaomi-plug/power", Jwt(), new { commandId = id ?? Guid.NewGuid(), on }));

    private async Task<HomeDto> Home(Guid id) => (await Read<DashboardDto>(await Send(HttpMethod.Get, Api, Jwt()))).Homes.Single(x => x.Id == id);

    [Test]
    public async Task HttpRequiresSignedOwnerAccessTokenAndCurrentCertificate()
    {
        Assert.That((await Send(HttpMethod.Get, Api + "/access")).StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        Assert.That((await Send(HttpMethod.Get, Api + "/access", Jwt())).StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That((await Send(HttpMethod.Get, Api + "/access", Jwt("other@example.test"))).StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
        Assert.That((await Send(HttpMethod.Get, Api + "/access", Jwt(thumbprint: "other@example.test-certificate"))).StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
        foreach (var invalid in new[] { "invalid", Jwt(accessToken: false), Jwt(audience: "other-client"),
            Jwt(clientId: "other-client"), Jwt(expired: true), Jwt(issuer: "https://other-issuer.example/openidc") })
            Assert.That((await Send(HttpMethod.Get, Api + "/access", invalid)).StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        using var attacker = RSA.Create(2048);
        Assert.That((await Send(HttpMethod.Get, Api + "/access", Jwt(signingKey: new RsaSecurityKey(attacker)))).StatusCode,
            Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task EnrollmentIsOneUseAndBoundToHome_AndAgentCannotBecomeUser()
    {
        var first = await CreateHome();
        var second = await CreateHome();
        Assert.That((await Send(HttpMethod.Post, Api + $"/agent/homes/{second.HomeId}/enroll", body: new { first.EnrollmentSecret })).StatusCode,
            Is.EqualTo(HttpStatusCode.Unauthorized));
        var token = await Enroll(first);
        Assert.That((await Send(HttpMethod.Post, Api + $"/agent/homes/{first.HomeId}/enroll", body: new { first.EnrollmentSecret })).StatusCode,
            Is.EqualTo(HttpStatusCode.Unauthorized));
        Assert.That((await Send(HttpMethod.Get, Api, token.Token)).StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await NewAgent(Jwt()).StartAsync());
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await NewAgent(new string('x', 43)).StartAsync());
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await NewAgent(token.Token, "?access_token=not-a-secret").StartAsync());
    }

    [Test]
    public async Task WebSocketEmulatorDistinguishesSentFromConfirmed_AndNeverRedelivers()
    {
        var token = await Enroll(await CreateHome());
        var agent = await Connect(token);
        Assert.That((await Send(HttpMethod.Post, Api + $"/homes/{token.HomeId}/devices/xiaomi-plug/power", Jwt(),
            new { commandId = Guid.NewGuid() })).StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That((await Home(token.HomeId)).Commands, Is.Empty);
        var command = await Queue(token.HomeId);
        Assert.That(command.Status, Is.EqualTo("queued"));
        var sent = await agent.InvokeAsync<ExchangeResponse>("Exchange", Snapshot());
        Assert.That(sent.Commands.Select(x => x.Id), Is.EqualTo(new[] { command.Id }));
        Assert.That((await Home(token.HomeId)).Commands.Single().Status, Is.EqualTo("sent"));
        Assert.That((await Queue(token.HomeId, command.Id)).Status, Is.EqualTo("sent"));
        Assert.That((await agent.InvokeAsync<ExchangeResponse>("Exchange", Snapshot())).Commands, Is.Empty);
        var result = Snapshot(true);
        result.Results.Add(new() { CommandId = command.Id, Status = "confirmed", Power = true, ObservedAt = clock.Now });
        await agent.InvokeAsync<ExchangeResponse>("Exchange", result);
        await agent.InvokeAsync<ExchangeResponse>("Exchange", result);
        var home = await Home(token.HomeId);
        Assert.That(home.Commands.Single().Status, Is.EqualTo("confirmed"));
        Assert.That(home.Devices.Single().Power, Is.True);
        Assert.That(home.Devices.Single().TemperatureCelsius, Is.EqualTo(26.5));
        Assert.That((await Send(HttpMethod.Post, Api + $"/homes/{token.HomeId}/devices/xiaomi-plug/power", Jwt(),
            new { commandId = command.Id, on = false })).StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
    }

    [Test]
    public async Task ForeignHomeAgentCannotReceiveOrAcknowledgeCommands()
    {
        var first = await Enroll(await CreateHome());
        var second = await Enroll(await CreateHome());
        var firstAgent = await Connect(first);
        var secondAgent = await Connect(second);
        var command = await Queue(first.HomeId);
        Assert.That((await secondAgent.InvokeAsync<ExchangeResponse>("Exchange", Snapshot())).Commands, Is.Empty);
        Assert.That((await firstAgent.InvokeAsync<ExchangeResponse>("Exchange", Snapshot())).Commands.Single().Id, Is.EqualTo(command.Id));
        var forged = Snapshot(true);
        forged.Results.Add(new() { CommandId = command.Id, Status = "confirmed", Power = true, ObservedAt = clock.Now });
        var error = await Assert.ThrowsAsync<HubException>(async () => await secondAgent.InvokeAsync<ExchangeResponse>("Exchange", forged));
        Assert.That(error.Message, Does.Contain("unknown_command"));
        Assert.That((await Home(first.HomeId)).Commands.Single().Status, Is.EqualTo("sent"));
        Assert.That((await Home(second.HomeId)).Devices.Single().Power, Is.False);
        Assert.That((await Send(HttpMethod.Post, Api + $"/homes/{first.HomeId}/revoke", Jwt("other@example.test"))).StatusCode,
            Is.EqualTo(HttpStatusCode.Forbidden));
        Assert.That((await Send(HttpMethod.Post, Api + "/homes", Jwt("other@example.test"), new { name = "Denied" })).StatusCode,
            Is.EqualTo(HttpStatusCode.Forbidden));
    }

    [Test]
    public async Task RevocationInvalidatesExistingConnectionAndRotationRequiresFreshSnapshot()
    {
        var token = await Enroll(await CreateHome());
        var agent = await Connect(token);
        await Queue(token.HomeId);
        await agent.InvokeAsync<ExchangeResponse>("Exchange", Snapshot());
        Assert.That((await Send(HttpMethod.Post, Api + $"/homes/{token.HomeId}/revoke", Jwt())).StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        await Assert.CatchAsync<Exception>(async () => await agent.InvokeAsync<ExchangeResponse>("Exchange", Snapshot(true)));
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await NewAgent(token.Token).StartAsync());
        var revoked = await Home(token.HomeId);
        Assert.That(revoked.Online, Is.False);
        Assert.That(revoked.Commands.Single().Status, Is.EqualTo("unknown"));
        Assert.That(revoked.Devices.Single().Power, Is.False);
        var enrollment = await Read<EnrollmentDto>(await Send(HttpMethod.Post, Api + $"/homes/{token.HomeId}/enrollment", Jwt()));
        var replacement = await Enroll(enrollment);
        Assert.That(replacement.Token, Is.Not.EqualTo(token.Token));
        var reconnected = NewAgent(replacement.Token);
        await reconnected.StartAsync();
        Assert.That((await Send(HttpMethod.Post, Api + $"/homes/{token.HomeId}/devices/xiaomi-plug/power", Jwt(),
            new { commandId = Guid.NewGuid(), on = true })).StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
        var exchange = await reconnected.InvokeAsync<ExchangeResponse>("Exchange", Snapshot());
        Assert.That(exchange.Commands, Is.Empty);
        Assert.That((await Home(token.HomeId)).Stale, Is.False);
    }

    [Test]
    public async Task LostConnectionPreservesReadingsAndNeverReplaysDeliveredCommand()
    {
        var token = await Enroll(await CreateHome());
        var agent = await Connect(token);
        await Queue(token.HomeId);
        await agent.InvokeAsync<ExchangeResponse>("Exchange", Snapshot());
        await agent.StopAsync();
        clock.Advance(21);
        var offline = await Home(token.HomeId);
        Assert.That(offline.Online, Is.False);
        Assert.That(offline.Stale, Is.True);
        Assert.That(offline.Commands.Single().Status, Is.EqualTo("unknown"));
        Assert.That(offline.Devices.Single().UpdatedAt, Is.EqualTo(clock.Now.AddSeconds(-21)));
        var reconnected = await Connect(token);
        Assert.That((await reconnected.InvokeAsync<ExchangeResponse>("Exchange", Snapshot())).Commands, Is.Empty);
        Assert.That((await Home(token.HomeId)).Commands.Single().Status, Is.EqualTo("unknown"));
    }

    [Test]
    public async Task TimeoutDoesNotBecomeConfirmation_AndStaleSnapshotsCannotRenewLease()
    {
        var token = await Enroll(await CreateHome());
        var agent = await Connect(token);
        var command = await Queue(token.HomeId);
        await agent.InvokeAsync<ExchangeResponse>("Exchange", Snapshot());
        clock.Advance(15);
        await agent.InvokeAsync<ExchangeResponse>("Exchange", Snapshot());
        clock.Advance(16);
        var late = Snapshot(true);
        late.Results.Add(new() { CommandId = command.Id, Status = "confirmed", Power = true, ObservedAt = clock.Now });
        Assert.That((await agent.InvokeAsync<ExchangeResponse>("Exchange", late)).Commands, Is.Empty);
        Assert.That((await Home(token.HomeId)).Commands.Single().Status, Is.EqualTo("unknown"));
        var stale = Snapshot();
        clock.Advance(31);
        Assert.That((await Assert.ThrowsAsync<HubException>(async () => await agent.InvokeAsync<ExchangeResponse>("Exchange", stale))).Message,
            Does.Contain("invalid_or_stale_snapshot"));
        var home = await Home(token.HomeId);
        Assert.That(home.Online, Is.False);
        Assert.That(home.Stale, Is.True);
        Assert.That((await Send(HttpMethod.Post, Api + $"/homes/{token.HomeId}/devices/xiaomi-plug/power", Jwt(),
            new { commandId = Guid.NewGuid(), on = true })).StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
    }

    [Test]
    public async Task UndeliveredCommandsExpireInsteadOfRunningAfterSilence()
    {
        var token = await Enroll(await CreateHome());
        var agent = await Connect(token);
        await Queue(token.HomeId);
        clock.Advance(31);
        Assert.That((await agent.InvokeAsync<ExchangeResponse>("Exchange", Snapshot())).Commands, Is.Empty);
        Assert.That((await Home(token.HomeId)).Commands.Single().Status, Is.EqualTo("expired"));
    }

    [Test]
    public async Task ExpiredEnrollmentAndAgentCredentialsAreRejected()
    {
        var enrollment = await CreateHome();
        clock.Advance(601);
        Assert.That((await Send(HttpMethod.Post, Api + $"/agent/homes/{enrollment.HomeId}/enroll", body: new { enrollment.EnrollmentSecret })).StatusCode,
            Is.EqualTo(HttpStatusCode.Unauthorized));
        var token = await Enroll(await CreateHome());
        var agent = await Connect(token);
        clock.Advance(91 * 24 * 60 * 60);
        await Assert.CatchAsync<Exception>(async () => await agent.InvokeAsync<ExchangeResponse>("Exchange", Snapshot()));
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await NewAgent(token.Token).StartAsync());
    }

    [Test]
    public async Task PlainHttpAndUrlCredentialsAreRejected()
    {
        Assert.That((await Send(HttpMethod.Get, Api + "/access?access_token=not-a-secret", Jwt())).StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That((await Send(HttpMethod.Get, "http://localhost" + Api + "/access", Jwt())).StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [TestCase("192.0.2.10")]
    [TestCase("::ffff:192.0.2.10")]
    public async Task TrustedProxyHttpHopSupportsOwnerEnrollmentAndAgentWebSocket(string address)
    {
        proxyPeer = IPAddress.Parse(address);
        Assert.That((await Send(HttpMethod.Get, "http://localhost" + Api + "/access", Jwt(), forwardedProto: "https")).StatusCode,
            Is.EqualTo(HttpStatusCode.OK));
        var enrollment = await Read<EnrollmentDto>(await Send(HttpMethod.Post, "http://localhost" + Api + "/homes", Jwt(),
            new { name = "Behind HAProxy" }, "https"));
        var token = await Read<AgentTokenDto>(await Send(HttpMethod.Post, "http://localhost" + Api + $"/agent/homes/{enrollment.HomeId}/enroll",
            body: new { enrollment.EnrollmentSecret }, forwardedProto: "https"));
        var agent = NewAgent(token.Token, forwardedHttp: true);
        await agent.StartAsync();
        await agent.InvokeAsync<ExchangeResponse>("Exchange", Snapshot());
        Assert.That((await Home(token.HomeId)).Online, Is.True);
    }

    [Test]
    public async Task UntrustedProxySpoofAndTrustedPlainHttpAreRejected()
    {
        proxyPeer = IPAddress.Parse("192.0.2.11");
        Assert.That((await Send(HttpMethod.Get, "http://localhost" + Api + "/access", Jwt(), forwardedProto: "https")).StatusCode,
            Is.EqualTo(HttpStatusCode.BadRequest));
        proxyPeer = IPAddress.Parse("192.0.2.10");
        Assert.That((await Send(HttpMethod.Get, "http://localhost" + Api + "/access", Jwt(), forwardedProto: "http")).StatusCode,
            Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That((await Send(HttpMethod.Get, "http://localhost" + Api + "/access", Jwt(), forwardedProto: "https, http")).StatusCode,
            Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task EnrollmentRateLimitsArePerActualClientBehindTrustedProxy()
    {
        proxyPeer = IPAddress.Parse("192.0.2.10");
        var enrollment = await CreateHome();
        var path = "http://localhost" + Api + $"/agent/homes/{enrollment.HomeId}/enroll";
        var wrongSecret = new { enrollmentSecret = new string('x', 43) };
        for (var attempt = 0; attempt < 10; attempt++)
            Assert.That((await Send(HttpMethod.Post, path, body: wrongSecret, forwardedProto: "https", forwardedFor: "198.51.100.20")).StatusCode,
                Is.EqualTo(HttpStatusCode.Unauthorized));
        Assert.That((await Send(HttpMethod.Post, path, body: wrongSecret, forwardedProto: "https", forwardedFor: "198.51.100.20")).StatusCode,
            Is.EqualTo(HttpStatusCode.TooManyRequests));
        // A forged left prefix cannot change the identity appended by HAProxy on the right.
        Assert.That((await Send(HttpMethod.Post, path, body: wrongSecret, forwardedProto: "https", forwardedFor: "203.0.113.99, 198.51.100.20")).StatusCode,
            Is.EqualTo(HttpStatusCode.TooManyRequests));
        var token = await Read<AgentTokenDto>(await Send(HttpMethod.Post, path, body: new { enrollment.EnrollmentSecret },
            forwardedProto: "https", forwardedFor: "198.51.100.21"));
        Assert.That(token.HomeId, Is.EqualTo(enrollment.HomeId));
    }

    [TestCase("localhost", null, "127.0.0.1", HttpStatusCode.OK)]
    [TestCase("nonexistent-passi-proxy.invalid", null, "127.0.0.1", HttpStatusCode.BadRequest)]
    [TestCase(" ", null, "127.0.0.1", HttpStatusCode.BadRequest)]
    [TestCase("localhost", "malformed", "127.0.0.1", HttpStatusCode.BadRequest)]
    [TestCase("localhost", "192.0.2.10", "127.0.0.1", HttpStatusCode.BadRequest)]
    public async Task ProxyDnsResolutionFailsClosedAndExplicitAddressesOverrideDns(
        string host, string addresses, string remote, HttpStatusCode expected)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string>
        {
            ["AppSetting:SmartHomeTrustedProxyHost"] = host,
            ["AppSetting:SmartHomeTrustedProxyAddresses"] = addresses
        }).Build();
        using var probe = new TestServer(new WebHostBuilder().ConfigureServices(services =>
            services.AddSingleton(new AppSetting(configuration) { PrefferAppsettingFile = true })).Configure(app =>
        {
            app.Use((context, next) => { context.Connection.RemoteIpAddress = IPAddress.Parse(remote); return next(); });
            app.UseMiddleware<SmartHomeForwardingMiddleware>();
            app.UseSmartHomeBoundary();
            app.Run(context => Microsoft.AspNetCore.Http.HttpResponseWritingExtensions.WriteAsync(context.Response,
                context.Request.Path == "/api/smarthome/peer" ? context.Connection.RemoteIpAddress.ToString() : context.Request.Scheme));
        }));
        using var probeClient = probe.CreateClient();
        probeClient.DefaultRequestHeaders.Add("X-Forwarded-Proto", "https");
        probeClient.DefaultRequestHeaders.Add("X-Forwarded-For", "203.0.113.99, 198.51.100.20");
        Assert.That((await probeClient.GetAsync("http://localhost/api/smarthome/access")).StatusCode, Is.EqualTo(expected));
        Assert.That(await probeClient.GetStringAsync("http://localhost/api/unrelated"), Is.EqualTo("http"));
        Assert.That(await probeClient.GetStringAsync("https://localhost/api/smarthome/peer"),
            Is.EqualTo(expected == HttpStatusCode.OK ? "198.51.100.20" : remote));
    }
}
