using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ConfigurationManager;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using NUnit.Framework;
using WebApp;
using WebApp.News;

namespace WebAppTests
{
    public class NewsMcpTests
    {
        private const string PublicBase = "https://passi.test";
        private const string Issuer = PublicBase + "/openidc";
        private const string McpClientId = "claude-code";
        private const string Admin = "admin@passi.cloud";

        private SqliteConnection _connection;
        private DbContextOptions<WebAppDbContext> _dbOptions;
        private RsaSecurityKey _signingKey;
        private WebApplication _app;
        private HttpClient _client;

        [SetUp]
        public async Task SetUp()
        {
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();
            _dbOptions = new DbContextOptionsBuilder<WebAppDbContext>().UseSqlite(_connection).Options;
            using (var db = new WebAppDbContext(_dbOptions))
                db.Database.EnsureCreated();

            _signingKey = new RsaSecurityKey(RSA.Create(2048)) { KeyId = "test" };

            var appSetting = new AppSetting(new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["AppSetting:IdentityUrlBase"] = PublicBase,
                    ["AppSetting:openIdcUrl"] = Issuer,
                    ["AppSetting:NewsAdminEmails"] = Admin,
                    ["AppSetting:NewsMcpClientIds"] = McpClientId,
                })
                .Build()) { PrefferAppsettingFile = true };

            var builder = WebApplication.CreateBuilder();
            builder.WebHost.UseTestServer();
            builder.Services.AddSingleton(appSetting);
            builder.Services.AddScoped(_ => new WebAppDbContext(_dbOptions));
            builder.Services.AddScoped<NewsService>();
            builder.Services.AddSingleton<NewsAdmins>();
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddAuthentication();
            builder.Services.AddNewsMcp(appSetting, backchannel: null);
            builder.Services.PostConfigure<JwtBearerOptions>(NewsMcp.BearerScheme, o =>
            {
                var configuration = new OpenIdConnectConfiguration { Issuer = Issuer };
                configuration.SigningKeys.Add(_signingKey);
                o.Configuration = configuration;
                o.ConfigurationManager = new StaticConfigurationManager<OpenIdConnectConfiguration>(configuration);
            });

            _app = builder.Build();
            _app.UseRouting();
            _app.UseAuthentication();
            _app.UseAuthorization();
            _app.MapNewsMcp();
            await _app.StartAsync();
            _client = _app.GetTestClient();
            _client.BaseAddress = new Uri(PublicBase);
        }

        [TearDown]
        public async Task TearDown()
        {
            _client.Dispose();
            await _app.DisposeAsync();
            _connection.Dispose();
        }

        private string Token(string email = Admin, string audience = McpClientId, string issuer = Issuer, SecurityKey key = null)
        {
            var descriptor = new SecurityTokenDescriptor
            {
                Issuer = issuer,
                Audience = audience,
                Expires = DateTime.UtcNow.AddMinutes(5),
                Subject = new ClaimsIdentity(new[] { new Claim("sub", email), new Claim("email", email), new Claim("client_id", audience) }),
                SigningCredentials = new SigningCredentials(key ?? _signingKey, SecurityAlgorithms.RsaSha256),
            };
            var handler = new JwtSecurityTokenHandler();
            return handler.WriteToken(handler.CreateToken(descriptor));
        }

        private async Task<HttpResponseMessage> Rpc(string token, string method, object @params = null)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "/mcp")
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(new { jsonrpc = "2.0", id = 1, method, @params = @params ?? new { } }),
                    Encoding.UTF8, "application/json"),
            };
            request.Headers.Accept.ParseAdd("application/json");
            request.Headers.Accept.ParseAdd("text/event-stream");
            request.Headers.Add("MCP-Protocol-Version", "2025-06-18");
            if (token != null)
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return await _client.SendAsync(request);
        }

        private static async Task<JsonElement> Result(HttpResponseMessage response)
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), await response.Content.ReadAsStringAsync());
            var body = await response.Content.ReadAsStringAsync();
            var json = body.TrimStart().StartsWith("{")
                ? body
                : body.Split('\n').Where(l => l.StartsWith("data:")).Select(l => l.Substring(5).Trim()).Last();
            var root = JsonDocument.Parse(json).RootElement;
            Assert.That(root.TryGetProperty("error", out var error), Is.False, error.ToString());
            return root.GetProperty("result");
        }

        private async Task<JsonElement> CallTool(string name, object arguments)
        {
            var result = await Result(await Rpc(Token(), "tools/call", new { name, arguments }));
            Assert.That(result.TryGetProperty("isError", out var isError) && isError.GetBoolean(), Is.False, result.ToString());
            return result;
        }

        [Test]
        public async Task ProtectedResourceMetadataPointsAtPassiAuthorizationServer()
        {
            var response = await _client.GetAsync("/.well-known/oauth-protected-resource/mcp");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
            Assert.That(json.GetProperty("resource").GetString(), Is.EqualTo(PublicBase + "/mcp"));
            Assert.That(json.GetProperty("authorization_servers").EnumerateArray().Select(e => e.GetString()), Does.Contain(Issuer));
        }

        [Test]
        public async Task MissingTokenGets401WithResourceMetadataChallenge()
        {
            var response = await Rpc(null, "tools/list");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
            Assert.That(response.Headers.WwwAuthenticate.ToString(), Does.Contain("resource_metadata="));
            Assert.That(response.Headers.WwwAuthenticate.ToString(), Does.Contain("/.well-known/oauth-protected-resource"));
        }

        [Test]
        public async Task TokenIssuedToAnotherClientIsRejected()
        {
            var response = await Rpc(Token(audience: "some-other-website"), "tools/list");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        }

        [Test]
        public async Task TokenFromAnotherIssuerOrKeyIsRejected()
        {
            Assert.That((await Rpc(Token(issuer: "https://evil.test/openidc"), "tools/list")).StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
            Assert.That((await Rpc(Token(key: new RsaSecurityKey(RSA.Create(2048))), "tools/list")).StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        }

        [Test]
        public async Task NonAdminIsForbidden()
        {
            var response = await Rpc(Token(email: "mallory@passi.cloud"), "tools/list");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
        }

        [Test]
        public async Task AdminSeesNewsTools()
        {
            var result = await Result(await Rpc(Token(), "tools/list"));

            var names = result.GetProperty("tools").EnumerateArray().Select(t => t.GetProperty("name").GetString()).ToList();
            Assert.That(names, Is.SupersetOf(new[] { "list_posts", "get_post", "create_post", "update_post", "publish_post", "unpublish_post", "delete_post" }));
        }

        [Test]
        public async Task AdminCanCreateUpdateAndPublishPostThroughTools()
        {
            await CallTool("create_post", new { title = "Agent post", summary = "From MCP", body_markdown = "Hello **agents**" });
            await CallTool("update_post", new { slug = "agent-post", summary = "Edited by MCP" });
            await CallTool("publish_post", new { slug = "agent-post" });

            using var db = new WebAppDbContext(_dbOptions);
            var post = await db.NewsPosts.SingleAsync();
            Assert.That(post.Slug, Is.EqualTo("agent-post"));
            Assert.That(post.Summary, Is.EqualTo("Edited by MCP"));
            Assert.That(post.BodyMarkdown, Is.EqualTo("Hello **agents**"));
            Assert.That(post.PublishedAt, Is.Not.Null);
            Assert.That(post.AuthorEmail, Is.EqualTo(Admin));

            var listed = await CallTool("list_posts", new { });
            Assert.That(listed.ToString(), Does.Contain("agent-post"));
        }

        [Test]
        public async Task ToolValidationErrorsAreReportedAsToolErrors()
        {
            var result = await Result(await Rpc(Token(), "tools/call", new { name = "publish_post", arguments = new { slug = "missing" } }));

            Assert.That(result.GetProperty("isError").GetBoolean(), Is.True);
            Assert.That(result.ToString(), Does.Contain("not found"));
        }
    }
}
