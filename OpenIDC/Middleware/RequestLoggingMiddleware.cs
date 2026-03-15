using Microsoft.AspNetCore.Http;
using NLog;
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

            _logger.Info($"Incoming {request.Method} {request.Path}{request.QueryString} from {context.Connection.RemoteIpAddress}");

            try
            {
                await _next(context);
            }
            finally
            {
                stopwatch.Stop();
                _logger.Info($"Completed {request.Method} {request.Path}{request.QueryString} - Status: {context.Response.StatusCode} - Duration: {stopwatch.ElapsedMilliseconds}ms");
            }
        }
    }
}
