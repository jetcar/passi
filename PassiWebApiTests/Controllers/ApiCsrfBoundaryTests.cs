using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using ConfigurationManager;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NUnit.Framework;
using passi_webapi;
using passi_webapi.Controllers;

namespace PassiWebApiTests.Controllers;

public class ApiCsrfBoundaryTests : TestBase
{
    private static readonly string[] Paths =
    {
        "/passiapi/api/Auth/Start",
        "/passiapi/api/Auth/Authorize",
        "/passiapi/api/Auth/Devices",
        "/passiapi/api/Auth/DeleteDevice",
        "/passiapi/api/Auth/UnlinkDevice",
        "/passiapi/api/SignUp/signup",
        "/passiapi/api/SignUp/Delete",
        "/passiapi/api/Certificate/UpdatePublicCert",
    };

    private IWebHost _host;
    private HttpClient _http;

    [OneTimeSetUp]
    public async Task StartHttpPipeline()
    {
        var configuration = new ConfigurationBuilder().AddJsonFile("test.appsettings.json").Build();
        var startup = new PassiApiStartup(configuration);

        // Reuse TestBase's isolated database/Redis settings, but exercise the real startup
        // and MVC formatter pipeline instead of calling controller methods directly.
        _host = new WebHostBuilder()
            .UseKestrel()
            .UseUrls("http://127.0.0.1:0")
            .ConfigureServices(services =>
            {
                startup.ConfigureServices(services);
                services.Replace(ServiceDescriptor.Singleton(ServiceProvider.GetRequiredService<AppSetting>()));
                services.AddControllers().AddApplicationPart(typeof(AuthController).Assembly);
            })
            .Configure(app => startup.Configure(app, app.ApplicationServices.GetRequiredService<IWebHostEnvironment>()))
            .Build();
        await _host.StartAsync();
        var address = _host.ServerFeatures.Get<IServerAddressesFeature>().Addresses.Single();
        _http = new HttpClient(new HttpClientHandler { UseCookies = false, AllowAutoRedirect = false })
        {
            BaseAddress = new Uri(address),
        };
    }

    [OneTimeTearDown]
    public async Task StopHttpPipeline()
    {
        _http?.Dispose();
        if (_host != null)
        {
            await _host.StopAsync();
            _host.Dispose();
        }
    }

    [Test, Combinatorial]
    public async Task BrowserSimplePostsAreRejectedByTheInputFormatter(
        [ValueSource(nameof(Paths))] string path,
        [Values("application/x-www-form-urlencoded", "multipart/form-data", "text/plain")] string mediaType)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, path)
        {
            Content = CreateSimpleContent(mediaType),
        };
        request.Headers.Add("Origin", "https://untrusted.example");
        using var response = await _http.SendAsync(request);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.UnsupportedMediaType),
            await response.Content.ReadAsStringAsync());
    }

    [Test]
    public async Task UntrustedJsonPreflightDoesNotGrantCorsPermission([ValueSource(nameof(Paths))] string path)
    {
        using var request = new HttpRequestMessage(HttpMethod.Options, path);
        request.Headers.Add("Origin", "https://untrusted.example");
        request.Headers.Add("Access-Control-Request-Method", "POST");
        request.Headers.Add("Access-Control-Request-Headers", "content-type");
        using var response = await _http.SendAsync(request);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.MethodNotAllowed));
        Assert.That(response.Headers.Contains("Access-Control-Allow-Origin"), Is.False);
        Assert.That(response.Headers.Contains("Access-Control-Allow-Credentials"), Is.False);
    }

    [Test]
    public async Task NativeJsonRequestReachesTokenValidationWithoutCookiesOrAntiforgeryToken()
    {
        var unknownToken = new string('0', 64);
        using var content = new StringContent($"{{\"UnlinkToken\":\"{unknownToken}\"}}", Encoding.UTF8, "application/json");
        using var response = await _http.PostAsync("/passiapi/api/Auth/UnlinkDevice", content);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That(await response.Content.ReadAsStringAsync(), Does.Contain("Invalid or expired unlink token"));
    }

    private static HttpContent CreateSimpleContent(string mediaType)
    {
        if (mediaType == "application/x-www-form-urlencoded")
        {
            return new FormUrlEncodedContent(new Dictionary<string, string> { ["UnlinkToken"] = "invalid" });
        }

        if (mediaType == "multipart/form-data")
        {
            return new MultipartFormDataContent { { new StringContent("invalid"), "UnlinkToken" } };
        }

        // A browser can submit JSON-shaped bytes as text/plain without a preflight.
        return new StringContent("{\"UnlinkToken\":\"invalid\"}", Encoding.UTF8, "text/plain");
    }
}
