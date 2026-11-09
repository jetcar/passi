using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using NLog;
using NLog.Config;
using NLog.Targets;
using NUnit.Framework;
using OpenIDC.Middleware;

namespace OpenIDCTests;

[NonParallelizable]
public class RequestLoggingMiddlewareTests
{
    [Test]
    public async Task EscapesLineBreaksInMethodAndPath()
    {
        var previous = LogManager.Configuration;
        var memory = new MemoryTarget { Layout = "${message}" };
        var configuration = new LoggingConfiguration();
        configuration.AddRule(LogLevel.Trace, LogLevel.Fatal, memory);
        LogManager.Configuration = configuration;
        try
        {
            var context = new DefaultHttpContext();
            context.Request.Method = "POST\r\nforged-method";
            context.Request.Path = new PathString("/connect/token\r\nforged-path");
            var middleware = new RequestLoggingMiddleware(_ => Task.CompletedTask);

            await middleware.InvokeAsync(context);

            Assert.That(memory.Logs, Has.Count.EqualTo(2));
            Assert.That(memory.Logs, Has.All.Contains("POST\\r\\nforged-method"));
            Assert.That(memory.Logs, Has.All.Contains("/connect/token\\r\\nforged-path"));
            Assert.That(memory.Logs, Has.None.Contains("\r"));
            Assert.That(memory.Logs, Has.None.Contains("\n"));
        }
        finally
        {
            LogManager.Configuration = previous;
        }
    }
}
