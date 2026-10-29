using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ConfigurationManager;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using WebApp.News;

namespace WebAppTests
{
    /// <summary>
    /// MCP clients that don't follow the protected-resource document (or ignore the /openidc path of the
    /// authorization server) look for authorization server metadata on passi.cloud itself. Without it they can't
    /// sign in or refresh tokens, so the news MCP looked "logged out" after every access token expiry.
    /// </summary>
    public class NewsMcpDiscoveryTests
    {
        private const string PublicBase = "https://passi.test";
        private const string OpenIdcUrl = "http://openidc.internal/openidc";
        private const string Metadata = "{\"issuer\":\"https://passi.test/openidc\",\"authorization_endpoint\":\"https://passi.test/openidc/login\",\"token_endpoint\":\"https://passi.test/openidc/connect/token\"}";

        private FakeOpenIdc _openIdc;
        private WebApplication _app;
        private HttpClient _client;

        private class FakeOpenIdc : HttpMessageHandler
        {
            public HttpStatusCode Status = HttpStatusCode.OK;
            public readonly List<Uri> Requests = new();

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                Requests.Add(request.RequestUri);
                var response = request.RequestUri?.ToString() == OpenIdcUrl + "/.well-known/openid-configuration"
                    ? new HttpResponseMessage(Status) { Content = new StringContent(Metadata, Encoding.UTF8, "application/json") }
                    : new HttpResponseMessage(HttpStatusCode.NotFound);
                return Task.FromResult(response);
            }
        }

        [SetUp]
        public async Task SetUp()
        {
            _openIdc = new FakeOpenIdc();
            var appSetting = new AppSetting(new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["AppSetting:IdentityUrlBase"] = PublicBase,
                    ["AppSetting:openIdcUrl"] = OpenIdcUrl,
                    ["AppSetting:NewsMcpClientIds"] = "claude-code",
                })
                .Build()) { PrefferAppsettingFile = true };

            var builder = WebApplication.CreateBuilder();
            builder.WebHost.UseTestServer();
            builder.Services.AddSingleton(appSetting);
            builder.Services.AddAuthentication();
            builder.Services.AddNewsMcp(appSetting, _openIdc);

            _app = builder.Build();
            _app.UseNewsMcpDiscovery();
            // Stand-in for the SPA fallback: anything that falls through is "index.html".
            _app.Run(context => context.Response.WriteAsync("<!DOCTYPE html>"));
            await _app.StartAsync();
            _client = _app.GetTestClient();
            _client.BaseAddress = new Uri(PublicBase);
        }

        [TearDown]
        public async Task TearDown()
        {
            _client.Dispose();
            await _app.DisposeAsync();
        }

        [TestCase("/.well-known/oauth-authorization-server")]
        [TestCase("/.well-known/oauth-authorization-server/mcp")]
        [TestCase("/.well-known/openid-configuration/mcp")]
        [TestCase("/mcp/.well-known/oauth-authorization-server")]
        [TestCase("/mcp/.well-known/openid-configuration")]
        public async Task AuthorizationServerMetadataIsServedAtFallbackDiscoveryPaths(string path)
        {
            var response = await _client.GetAsync(path);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(response.Content.Headers.ContentType?.MediaType, Is.EqualTo("application/json"));
            var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
            Assert.That(json.GetProperty("issuer").GetString(), Is.EqualTo("https://passi.test/openidc"));
            Assert.That(json.GetProperty("token_endpoint").GetString(), Is.EqualTo("https://passi.test/openidc/connect/token"));
        }

        [Test]
        public async Task UnknownWellKnownPathUnderMcpIsNotFoundInsteadOfTheSpa()
        {
            var response = await _client.GetAsync("/mcp/.well-known/something-else");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        }

        [Test]
        public async Task OpenIdcFailureIsReportedAsBadGateway()
        {
            _openIdc.Status = HttpStatusCode.InternalServerError;

            var response = await _client.GetAsync("/.well-known/oauth-authorization-server");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadGateway));
        }

        [Test]
        public async Task OtherPathsPassThrough()
        {
            var response = await _client.GetAsync("/news");

            Assert.That(await response.Content.ReadAsStringAsync(), Is.EqualTo("<!DOCTYPE html>"));
            Assert.That(_openIdc.Requests, Is.Empty);
        }
    }
}
