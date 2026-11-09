using Microsoft.AspNetCore.Http;
using NLog;
using OpenIDC.Helpers;
using System.Diagnostics;
using System.Threading.Tasks;

namespace OpenIDC.Middleware
{
    public class RequestLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private static readonly ILogger _logger = LogManager.GetCurrentClassLogger();

        public RequestLoggingMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var stopwatch = Stopwatch.StartNew();
            var request = context.Request;

            var method = LogValue.Safe(request.Method);
            var path = LogValue.Safe(request.Path.Value);
            _logger.Info($"Incoming {method} {path} from {context.Connection.RemoteIpAddress}");

            try
            {
                await _next(context);
            }
            finally
            {
                stopwatch.Stop();
                _logger.Info($"Completed {method} {path} - Status: {context.Response.StatusCode} - Duration: {stopwatch.ElapsedMilliseconds}ms");
            }
        }
    }
}
