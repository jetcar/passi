using Microsoft.AspNetCore.Http;
using NLog;
using System.Diagnostics;
using System.Threading.Tasks;

namespace passi_webapi.Middleware
{
    public class RequestLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private static readonly ILogger _logger = LogManager.GetCurrentClassLogger();

        public RequestLoggingMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        // Prevent request values from injecting additional entries into text logs.
        private static string SafeLogValue(string value) => value?.Replace("\r", "\\r").Replace("\n", "\\n");

        public async Task InvokeAsync(HttpContext context)
        {
            var stopwatch = Stopwatch.StartNew();
            var request = context.Request;
            var query = request.Path.StartsWithSegments("/api/smarthome") ? "" : SafeLogValue(request.QueryString.ToString());
            var method = SafeLogValue(request.Method);
            var path = SafeLogValue(request.Path.Value);

            _logger.Info($"Incoming {method} {path}{query} from {context.Connection.RemoteIpAddress}");

            try
            {
                await _next(context);
            }
            finally
            {
                stopwatch.Stop();
                _logger.Info($"Completed {method} {path}{query} - Status: {context.Response.StatusCode} - Duration: {stopwatch.ElapsedMilliseconds}ms");
            }
        }
    }
}
