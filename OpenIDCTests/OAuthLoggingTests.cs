using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NLog;
using NLog.Config;
using NLog.Targets;
using NUnit.Framework;
using OpenIDC.Controllers;
using OpenIDC.Middleware;

namespace OpenIDCTests;

[NonParallelizable]
public class OAuthLoggingTests
{
    [Test]
    public async Task RequestLogsExcludeQueryHeadersBodiesAndTokenResponse()
    {
        var previous = LogManager.Configuration;
        var memory = new MemoryTarget { Layout = "${message}" };
        var configuration = new LoggingConfiguration();
        configuration.AddRule(LogLevel.Trace, LogLevel.Fatal, memory);
        LogManager.Configuration = configuration;
        try
        {
            var context = new DefaultHttpContext();
            context.Request.Method = "POST";
            context.Request.Path = "/connect/token";
            context.Request.QueryString = new QueryString("?code=secret-code-123&code_verifier=secret-verifier&access_token=secret-access");
            context.Request.Headers.Authorization = "Bearer secret-header";
            context.Request.Body = new System.IO.MemoryStream(System.Text.Encoding.UTF8.GetBytes("refresh_token=secret-refresh"));
            context.Response.Body = new System.IO.MemoryStream();
            var middleware = new RequestLoggingMiddleware(async ctx => await ctx.Response.WriteAsync("secret-response-token"));
            await middleware.InvokeAsync(context);
            Assert.That(memory.Logs.Count, Is.EqualTo(2));
            Assert.That(memory.Logs, Has.All.Contains("/connect/token"));
            Assert.That(memory.Logs, Has.None.Contains("secret-"));
        }
        finally { LogManager.Configuration = previous; }
    }

    [TestCase("openid", false)]
    [TestCase("openid email", true)]
    public async Task UserInfoRespectsEmailScope(string scope, bool expectsEmail)
    {
        var controller = new AuthorizationController(null, null, null, null, DynamicRegistrationTests.Settings())
        {
            ControllerContext = new() { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                { new Claim("sub", "subject"), new Claim(ClaimTypes.Name, "alice@passi.cloud"), new Claim("scope", scope) }, "Bearer")) } }
        };
        var response = (OkObjectResult)await controller.UserInfo();
        var json = System.Text.Json.JsonSerializer.SerializeToElement(response.Value);
        Assert.That(json.TryGetProperty("email", out _), Is.EqualTo(expectsEmail));
    }
}
