using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ConfigurationManager;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using WebApp.Controllers;
using WebApp.Services;

namespace WebAppTests;

public class SmartHomeBffTests
{
    private WebApplication _app;
    private HttpClient _client;
    private Downstream _downstream;
    private string _cookie;

    [SetUp]
    public async Task Setup()
    {
        _downstream = new Downstream();
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddLogging();
        builder.Services.AddAuthentication("Cookies").AddCookie("Cookies");
        builder.Services.AddAntiforgery(o => o.HeaderName = "RequestVerificationToken");
        builder.Services.AddControllersWithViews().AddApplicationPart(typeof(SmartHomeController).Assembly);
        var settings = new AppSetting(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string> {
            ["AppSetting:SmartHomeApiBaseUrl"] = "https://cloud.test/passiapi"
        }).Build()) { PrefferAppsettingFile = true };
        builder.Services.AddSingleton(settings);
        builder.Services.AddTransient(_ => new SmartHomeClient(new HttpClient(_downstream, false), settings));
        _app = builder.Build();
        _app.UseRouting();
        _app.UseAuthentication();
        _app.UseAuthorization();
        // Test-only login issues a real protected cookie with the saved access token.
        _app.MapGet("/test-login", async context => {
            var properties = new AuthenticationProperties();
            properties.StoreTokens(new[] { new AuthenticationToken { Name = "access_token", Value = "saved-user-token" } });
            await context.SignInAsync("Cookies", new ClaimsPrincipal(new ClaimsIdentity(
                new[] { new Claim(ClaimTypes.NameIdentifier, "signed-in-user") }, "Cookies")), properties);
        });
        _app.MapControllers();
        await _app.StartAsync();
        _client = _app.GetTestClient();
        _client.BaseAddress = new Uri("https://web.test");
        _cookie = null;
    }

    [TearDown]
    public async Task Teardown()
    {
        _client.Dispose();
        await _app.DisposeAsync();
    }

    private async Task Login()
    {
        using var response = await _client.GetAsync("/test-login");
        _cookie = string.Join("; ", ExtractCookies(response));
        _client.DefaultRequestHeaders.Add("Cookie", _cookie);
    }

    private static IEnumerable<string> ExtractCookies(HttpResponseMessage response)
    {
        if (response.Headers.TryGetValues("Set-Cookie", out var cookies))
            foreach (var cookie in cookies) yield return cookie.Split(';')[0];
    }

    private async Task<string> Csrf()
    {
        using var response = await _client.GetAsync("/api/smarthome/csrf");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        _client.DefaultRequestHeaders.Remove("Cookie");
        _client.DefaultRequestHeaders.Add("Cookie", _cookie + "; " + string.Join("; ", ExtractCookies(response)));
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return json.RootElement.GetProperty("token").GetString();
    }

    [Test]
    public async Task AnonymousRequestCannotForwardSuppliedAuthorization()
    {
        _client.DefaultRequestHeaders.Add("Authorization", "Bearer client-forged-token");
        using var response = await _client.GetAsync("/api/smarthome");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        Assert.That(_downstream.Calls, Is.Zero);
    }

    [Test]
    public async Task OnlySavedTokenIsForwardedAndForeignUserIsDeniedByApi()
    {
        await Login();
        _client.DefaultRequestHeaders.Add("Authorization", "Bearer client-forged-token");
        _downstream.Status = HttpStatusCode.Forbidden;
        using var response = await _client.GetAsync("/api/smarthome?email=admin@passi.cloud&userId=1");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
        Assert.That(_downstream.Authorization, Is.EqualTo("Bearer saved-user-token"));
        Assert.That(_downstream.Url, Is.EqualTo("https://cloud.test/passiapi/api/smarthome/"));
        Assert.That(response.Headers.CacheControl?.NoStore, Is.True);
    }

    [Test]
    public async Task MissingCsrfCannotCreateHome()
    {
        await Login();
        using var response = await _client.PostAsync("/api/smarthome/homes", Json("{\"name\":\"Home\"}"));
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That(_downstream.Calls, Is.Zero);
    }

    [Test]
    public async Task CommandTimeoutIsNotRetried()
    {
        await Login();
        var csrf = await Csrf();
        _downstream.Calls = 0;
        _downstream.Timeout = true;
        using var request = new HttpRequestMessage(HttpMethod.Post,
            $"/api/smarthome/homes/{Guid.NewGuid()}/devices/plug/power") {
            Content = Json($"{{\"commandId\":\"{Guid.NewGuid()}\",\"on\":true}}")
        };
        request.Headers.Add("RequestVerificationToken", csrf);
        using var response = await _client.SendAsync(request);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadGateway));
        Assert.That(_downstream.Calls, Is.EqualTo(1));
        Assert.That(await response.Content.ReadAsStringAsync(), Does.Not.Contain("saved-user-token"));
    }

    [Test]
    public async Task ValidCsrfForwardsOneFixedWriteAndDoesNotRelayRedirects()
    {
        await Login();
        var csrf = await Csrf();
        _downstream.Calls = 0;
        _downstream.Status = HttpStatusCode.Redirect;
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/smarthome/homes") { Content = Json("{\"name\":\"Home\"}") };
        request.Headers.Add("RequestVerificationToken", csrf);
        using var response = await _client.SendAsync(request);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadGateway));
        Assert.That(response.Headers.Location, Is.Null);
        Assert.That(_downstream.Calls, Is.EqualTo(1));
        Assert.That(_downstream.Url, Is.EqualTo("https://cloud.test/passiapi/api/smarthome/homes"));
    }

    private static StringContent Json(string body) => new(body, Encoding.UTF8, "application/json");

    [TestCase("schedule")]
    [TestCase("schedule/resume")]
    public async Task ScheduleWritesRequireCsrf(string suffix)
    {
        await Login();
        using var response = await _client.PostAsync(
            $"/api/smarthome/homes/{Guid.NewGuid()}/devices/plug/{suffix}", Json("{}"));
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That(_downstream.Calls, Is.Zero);
    }

    [TestCase("schedule", HttpStatusCode.Conflict)]
    [TestCase("schedule/resume", HttpStatusCode.Forbidden)]
    public async Task ScheduleWritesPreserveVersionBodyAndServerDenial(string suffix, HttpStatusCode status)
    {
        await Login();
        var csrf = await Csrf();
        var home = Guid.NewGuid();
        var body = $"{{\"commandId\":\"{Guid.NewGuid()}\",\"expectedVersion\":7,\"expectedRuntimeRevision\":12}}";
        _downstream.Calls = 0;
        _downstream.Status = status;
        using var request = new HttpRequestMessage(HttpMethod.Post,
            $"/api/smarthome/homes/{home}/devices/xiaomi-plug-123/{suffix}") { Content = Json(body) };
        request.Headers.Add("RequestVerificationToken", csrf);
        request.Headers.Add("Authorization", "Bearer client-forged-token");
        using var response = await _client.SendAsync(request);
        Assert.That(response.StatusCode, Is.EqualTo(status));
        Assert.That(_downstream.Calls, Is.EqualTo(1));
        Assert.That(_downstream.Authorization, Is.EqualTo("Bearer saved-user-token"));
        Assert.That(_downstream.Url, Is.EqualTo($"https://cloud.test/passiapi/api/smarthome/homes/{home}/devices/xiaomi-plug-123/{suffix}"));
        Assert.That(_downstream.Body, Is.EqualTo(body));
        Assert.That(response.Headers.CacheControl?.NoStore, Is.True);
    }

    [TestCase("schedule")]
    [TestCase("schedule/resume")]
    public async Task ScheduleWriteTimeoutNeverRetries(string suffix)
    {
        await Login();
        var csrf = await Csrf();
        _downstream.Calls = 0;
        _downstream.Timeout = true;
        using var request = new HttpRequestMessage(HttpMethod.Post,
            $"/api/smarthome/homes/{Guid.NewGuid()}/devices/plug/{suffix}") { Content = Json("{}") };
        request.Headers.Add("RequestVerificationToken", csrf);
        using var response = await _client.SendAsync(request);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadGateway));
        Assert.That(_downstream.Calls, Is.EqualTo(1));
    }

    private sealed class Downstream : HttpMessageHandler
    {
        public int Calls;
        public string Authorization;
        public string Url;
        public string Body;
        public bool Timeout;
        public HttpStatusCode Status = HttpStatusCode.OK;
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls++;
            Authorization = request.Headers.Authorization?.ToString();
            Url = request.RequestUri.ToString();
            Body = request.Content == null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
            if (Timeout) throw new TaskCanceledException();
            var response = new HttpResponseMessage(Status) { Content = Json("{\"allowed\":true}") };
            if (Status == HttpStatusCode.Redirect) response.Headers.Location = new Uri("https://other.test");
            return response;
        }
    }
}
