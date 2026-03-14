using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;

namespace Services
{
    /// <summary>
    /// Middleware to generate and track correlation IDs for each request.
    /// The correlation ID will be available in all logs and can be used to track requests across systems.
    /// </summary>
    public class CorrelationIdMiddleware
    {
        private const string CorrelationIdHeaderName = "X-Correlation-Id";
        private const string CorrelationIdPropertyName = "CorrelationId";
        private readonly RequestDelegate _next;

        public CorrelationIdMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Try to get correlation ID from request header, or generate a new one
            var correlationId = context.Request.Headers[CorrelationIdHeaderName].ToString();

            if (string.IsNullOrEmpty(correlationId))
            {
                correlationId = Guid.NewGuid().ToString();
            }

            // Set correlation ID in Log4net's LogicalThreadContext (supports async operations)
            log4net.LogicalThreadContext.Properties[CorrelationIdPropertyName] = correlationId;

            // Also add to HttpContext for other middleware/controllers to access
            context.Items[CorrelationIdPropertyName] = correlationId;

            // Add correlation ID to response headers for client tracking
            context.Response.OnStarting(() =>
            {
                context.Response.Headers[CorrelationIdHeaderName] = correlationId;
                return Task.CompletedTask;
            });

            await _next(context);

            // Clean up after request
            log4net.LogicalThreadContext.Properties.Remove(CorrelationIdPropertyName);
        }
    }
}
