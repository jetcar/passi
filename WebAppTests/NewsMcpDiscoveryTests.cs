using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using NUnit.Framework;
using WebApp.News;

namespace WebAppTests
{
    /// <summary>
    /// /mcp is a protected resource. Authorization server metadata for /openidc must not be served
    /// at discovery paths that make clients expect /mcp or the host root as the issuer.
    /// </summary>
    public class NewsMcpDiscoveryTests
    {
        private const string PublicBase = "https://passi.test";
        private WebApplication _app;
        private HttpClient _client;

        [SetUp]
        public async Task SetUp()
        {
            var builder = WebApplication.CreateBuilder();
            builder.WebHost.UseTestServer();

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
        public async Task InvalidAuthorizationServerDiscoveryPathReturns404(string path)
        {
            var response = await _client.GetAsync(path);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        }

        [Test]
        public async Task UnknownWellKnownPathUnderMcpIsNotFoundInsteadOfTheSpa()
        {
            var response = await _client.GetAsync("/mcp/.well-known/something-else");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        }

        [Test]
        public async Task OtherPathsPassThrough()
        {
            var response = await _client.GetAsync("/news");

            Assert.That(await response.Content.ReadAsStringAsync(), Is.EqualTo("<!DOCTYPE html>"));
        }
    }
}
