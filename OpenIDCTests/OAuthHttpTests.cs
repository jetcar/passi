using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using NUnit.Framework;
using OpenIDC.Controllers;
using OpenIDC.Models;
using OpenIDC.Services;
using RedisClient;
using RestSharp;
using Services;
using WebApiDto;
using WebApiDto.Auth;
using WebApiDto.Auth.Dto;

namespace OpenIDCTests;

public class OAuthHttpTests
{
    private TestServer _server;
    private HttpClient _http;
    private X509Certificate2 _certificate;
    private RSA _rsa;
    private PhoneBackend _phone;
    private ProtocolRedis _redis;
    private const string Verifier = "verifier-0123456789-0123456789-0123456789-abcdef";
    private const string Redirect = "http://127.0.0.1:4242/callback";
    private const string Email = "alice@passi.cloud";

    [SetUp]
    public void SetUp()
    {
        _rsa = RSA.Create(2048);
        _certificate = new CertificateRequest("CN=Passi HTTP test", _rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1)
            .CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1));
        _redis = new(); _phone = new(_certificate, _rsa);
        var settings = DynamicRegistrationTests.Settings(new()
        { ["AppSetting:TrustedProxyHosts"] = "127.0.0.2", ["AppSetting:checkRequest"] = "/api/auth/check", ["AppSetting:startRequest"] = "/api/auth/start" });
        var startup = new Startup(new ConfigurationBuilder().Build());
        _server = new TestServer(new WebHostBuilder().ConfigureServices(services =>
        {
            services.AddLogging();
            services.AddControllersWithViews().AddApplicationPart(typeof(TokenController).Assembly);
            services.AddSingleton(settings);
            services.AddSingleton<IRedisService>(_redis);
            services.AddSingleton<IRandomGenerator, RandomGenerator>();
            services.AddSingleton<IMyRestClient>(_phone);
            services.AddSingleton<IClientStore, ClientStore>();
            services.AddSingleton<IAuthorizationCodeStore, AuthorizationCodeStore>();
            services.AddSingleton<IRefreshTokenStore, RefreshTokenStore>();
            services.AddSingleton<ILoginStartTokenService, LoginStartTokenService>();
            services.AddSingleton<ILoginRateLimiter, LoginRateLimiter>();
            services.AddSingleton<ResourcePolicy>();
            services.AddSingleton<DynamicClientRegistration>();
            services.AddSingleton(new SigningCredentials(new X509SecurityKey(_certificate), SecurityAlgorithms.RsaSha256));
            services.AddSingleton<ITokenService, TokenService>();
            services.AddAuthentication(); services.AddAuthorization();
            services.AddCors(options => options.AddPolicy("AllowOIDCClients", builder => builder.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));
        }).Configure(app =>
        {
            app.Use((context, next) => { context.Connection.RemoteIpAddress = IPAddress.Parse("192.0.2.30"); return next(); });
            startup.Configure(app, null);
        }));
        _http = _server.CreateClient(); _http.BaseAddress = new Uri("https://passi.cloud");
    }

    [TearDown]
    public void TearDown() { _http.Dispose(); _server.Dispose(); _certificate.Dispose(); _rsa.Dispose(); }

    private async Task<string> Register()
    {
        var response = await _http.PostAsJsonAsync("/openidc/connect/register", DynamicRegistrationTests.Request());
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created), await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("client_id").GetString();
    }

    private static string AuthorizationUrl(string clientId, string resource = ResourcePolicy.Finance, string method = "S256", string challenge = null) =>
        QueryHelpers.AddQueryString("/openidc/api/authorize", new Dictionary<string, string>
        {
            ["client_id"] = clientId, ["redirect_uri"] = Redirect, ["response_type"] = "code", ["scope"] = "email",
            ["state"] = "client-state", ["code_challenge"] = challenge ?? Base64UrlEncoder.Encode(SHA256.HashData(Encoding.ASCII.GetBytes(Verifier))),
            ["code_challenge_method"] = method, ["resource"] = resource,
        });

    [TestCase("/openidc/.well-known/openid-configuration")]
    [TestCase("/openidc/.well-known/oauth-authorization-server")]
    [TestCase("/.well-known/oauth-authorization-server/openidc")]
    [TestCase("/.well-known/openid-configuration/openidc")]
    public async Task IssuerDiscoveryRoutesPublishSameEndpoints(string path)
    {
        var response = await _http.GetAsync(path);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.That(body.GetProperty("issuer").GetString(), Is.EqualTo("https://passi.cloud/openidc"));
        Assert.That(body.GetProperty("registration_endpoint").GetString(), Is.EqualTo("https://passi.cloud/openidc/connect/register"));
        Assert.That(body.GetProperty("code_challenge_methods_supported").EnumerateArray().Select(x => x.GetString()), Is.EqualTo(new[] { "S256" }));
        Assert.That(body.GetProperty("grant_types_supported").EnumerateArray().Select(x => x.GetString()), Does.Contain("refresh_token"));
        Assert.That((await _http.GetAsync(body.GetProperty("jwks_uri").GetString())).StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(response.Headers.GetValues("X-Frame-Options").Single(), Is.EqualTo("DENY"));
    }

    [Test]
    public async Task CompletePhoneLoginBindsResourceAndExchangesCodeUsingDiscoveredJwks()
    {
        var clientId = await Register();
        var url = AuthorizationUrl(clientId);
        _http.DefaultRequestHeaders.Add("Sec-Fetch-Site", "same-origin");
        var preflight = await _http.GetAsync(url);
        Assert.That(preflight.StatusCode, Is.EqualTo(HttpStatusCode.OK), await preflight.Content.ReadAsStringAsync());
        var body = await preflight.Content.ReadFromJsonAsync<JsonElement>();
        Assert.That(body.GetProperty("resource").GetString(), Is.EqualTo(ResourcePolicy.Finance));
        Assert.That(body.GetProperty("client_name").GetString(), Is.EqualTo(DynamicRegistrationTests.Request().ClientName));
        Assert.That(body.GetProperty("consent_required").GetBoolean(), Is.True);
        _http.DefaultRequestHeaders.Add("Cookie", preflight.Headers.GetValues("Set-Cookie").Single().Split(';')[0]);
        _http.DefaultRequestHeaders.Add("X-Passi-Login-Token", body.GetProperty("login_token").GetString());
        var login = await _http.PostAsync(url.Replace("/authorize?", "/login?") + "&username=" + Email, null);
        Assert.That(login.StatusCode, Is.EqualTo(HttpStatusCode.OK), await login.Content.ReadAsStringAsync());
        _http.DefaultRequestHeaders.Remove("X-Passi-Login-Token");
        var checkUrl = url.Replace("/authorize?", "/check?") + "&sessionId=" + _phone.SessionId;
        // Omitting the original resource cannot turn the phone approval into a differently scoped grant.
        var changed = checkUrl.Replace("&resource=" + Uri.EscapeDataString(ResourcePolicy.Finance), "");
        Assert.That((await _http.PostAsync(changed, null)).StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        var check = await _http.PostAsync(checkUrl, null);
        Assert.That(check.StatusCode, Is.EqualTo(HttpStatusCode.OK), await check.Content.ReadAsStringAsync());
        var redirect = (await check.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("redirect_url").GetString();
        var query = QueryHelpers.ParseQuery(new Uri(redirect).Query);
        Assert.That(query["state"].ToString(), Is.EqualTo("client-state"));
        Assert.That((await _http.PostAsync(checkUrl, null)).StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        var exchange = new Dictionary<string, string> { ["grant_type"] = "authorization_code", ["client_id"] = clientId,
            ["code"] = query["code"], ["redirect_uri"] = Redirect, ["code_verifier"] = Verifier, ["resource"] = ResourcePolicy.Finance };
        var token = await _http.PostAsync("/openidc/connect/token", new FormUrlEncodedContent(exchange));
        Assert.That(token.StatusCode, Is.EqualTo(HttpStatusCode.OK), await token.Content.ReadAsStringAsync());
        Assert.That(token.Headers.CacheControl.NoStore, Is.True);
        var tokens = await token.Content.ReadFromJsonAsync<JsonElement>();
        var jwks = await _http.GetStringAsync("/openidc/.well-known/jwks");
        var parameters = new TokenValidationParameters { ValidIssuer = "https://passi.cloud/openidc", ValidAudience = ResourcePolicy.Finance,
            IssuerSigningKeys = new JsonWebKeySet(jwks).GetSigningKeys(), ValidateLifetime = true, ValidAlgorithms = new[] { "RS256" } };
        new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().ValidateToken(tokens.GetProperty("access_token").GetString(), parameters, out var verified);
        var jwt = (System.IdentityModel.Tokens.Jwt.JwtSecurityToken)verified;
        Assert.That(jwt.Payload["email"], Is.EqualTo(Email));
        Assert.That(jwt.Payload["client_id"], Is.EqualTo(clientId));
        Assert.That(jwt.Payload["scope"], Is.EqualTo("email"));
        Assert.That((await _http.PostAsync("/openidc/connect/token", new FormUrlEncodedContent(exchange))).StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [TestCase("plain", null)]
    [TestCase(null, null)]
    [TestCase("S256", "too-short")]
    public async Task AuthorizationRejectsNonS256OrMalformedChallenges(string method, string challenge)
    {
        _http.DefaultRequestHeaders.Add("Sec-Fetch-Site", "same-origin");
        var response = await _http.GetAsync(AuthorizationUrl(await Register(), method: method, challenge: challenge));
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [TestCase("https://evil.example/mcp")]
    [TestCase("")]
    public async Task AuthorizationRejectsUnknownOrEmptyResource(string resource)
    {
        _http.DefaultRequestHeaders.Add("Sec-Fetch-Site", "same-origin");
        var response = await _http.GetAsync(AuthorizationUrl(await Register(), resource));
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That((await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("error").GetString(), Is.EqualTo("invalid_target"));
    }

    [Test]
    public async Task RegistrationLimitReturnsRetryAfterAndFailsClosedOnStorageFailure()
    {
        for (var i = 0; i < 5; i++) await Register();
        var response = await _http.PostAsJsonAsync("/openidc/connect/register", DynamicRegistrationTests.Request());
        Assert.That((int)response.StatusCode, Is.EqualTo(429));
        Assert.That(response.Headers.RetryAfter, Is.Not.Null);
        _redis.Unavailable = true;
        response = await _http.PostAsJsonAsync("/openidc/connect/register", DynamicRegistrationTests.Request());
        Assert.That((int)response.StatusCode, Is.EqualTo(503));
    }

    [Test]
    public async Task TokenRejectsDuplicateAndExplicitlyEmptyResource()
    {
        foreach (var data in new[] { "resource=", "resource=" + Uri.EscapeDataString(ResourcePolicy.Finance) + "&resource=" + Uri.EscapeDataString(ResourcePolicy.Finance) })
        {
            var response = await _http.PostAsync("/openidc/connect/token", new StringContent("client_id=x&grant_type=authorization_code&code=x&" + data,
                Encoding.UTF8, "application/x-www-form-urlencoded"));
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        }
    }

    private sealed class ProtocolRedis : IRedisService
    {
        private readonly ConcurrentDictionary<string, object> _values = new();
        private readonly ConcurrentDictionary<string, int> _counts = new();
        public bool Unavailable { get; set; }
        private static string Key<T>(string key) => typeof(T).FullName + key;
        public void Add<T>(string key, T value, TimeSpan expire) => _values[Key<T>(key)] = value;
        public void Add<T>(string key, T value) => Add(key, value, TimeSpan.FromMinutes(5));
        public T Get<T>(string key) => _values.TryGetValue(Key<T>(key), out var value) ? (T)value : default;
        public T GetAndDelete<T>(string key) => _values.TryRemove(Key<T>(key), out var value) ? (T)value : default;
        public void Delete<T>(string key) => _values.TryRemove(Key<T>(key), out _);
        public bool TryAddBounded<T>(string bucket, string key, T item, int capacity, TimeSpan lifetime) => _values.TryAdd(Key<T>(key), item);
        public bool TryAcquireFixedWindow(string key, int permits, TimeSpan window, out TimeSpan retry)
        {
            if (Unavailable) throw new InvalidOperationException("unavailable");
            retry = window; return _counts.AddOrUpdate(key, 1, (_, value) => value + 1) <= permits;
        }
    }

    private sealed class PhoneBackend : IMyRestClient
    {
        public Guid SessionId { get; } = Guid.NewGuid();
        private readonly X509Certificate2 _certificate;
        private readonly RSA _rsa;
        private string _random;
        public PhoneBackend(X509Certificate2 certificate, RSA rsa) { _certificate = certificate; _rsa = rsa; }
        public Task<RestResponse> ExecuteAsync(RestRequest request)
        {
            object response;
            if (request.Resource == "/api/auth/start")
            {
                _random = ((StartLoginDto)request.Parameters.OfType<BodyParameter>().Single().Value).RandomString;
                response = new LoginResponceDto { SessionId = SessionId };
            }
            else if (request.Resource.StartsWith("/api/auth/check"))
                response = new CheckResponceDto { Username = Email, PublicCertThumbprint = "thumb" };
            else if (request.Resource.StartsWith("api/Certificate/Public"))
                response = new CertificateDto { PublicCert = Convert.ToBase64String(_certificate.RawData), Thumbprint = "thumb" };
            else response = new SessionMinDto { RandomString = _random,
                SignedHash = Convert.ToBase64String(_rsa.SignHash(SHA512.HashData(Encoding.ASCII.GetBytes(_random)), HashAlgorithmName.SHA512, RSASignaturePadding.Pkcs1)) };
            return Task.FromResult(new RestResponse(request) { StatusCode = HttpStatusCode.OK, IsSuccessStatusCode = true,
                ResponseStatus = ResponseStatus.Completed, Content = Newtonsoft.Json.JsonConvert.SerializeObject(response) });
        }
    }
}
