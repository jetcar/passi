using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;
using NLog;

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

        private const int MaxCorrelationIdLength = 64;

        public async Task InvokeAsync(HttpContext context)
        {
            // Try to get correlation ID from request header, or generate a new one.
            // The header is client-supplied, so only accept it if it looks like an
            // identifier - otherwise an attacker could use it to inject arbitrary
            // content into every log line for the request or bloat log output.
            var correlationId = context.Request.Headers[CorrelationIdHeaderName].ToString();

            if (!IsValidCorrelationId(correlationId))
            {
                correlationId = Guid.NewGuid().ToString();
            }

            // Set correlation ID in log4net's LogicalThreadContext (supports async operations)
            log4net.LogicalThreadContext.Properties[CorrelationIdPropertyName] = correlationId;

            // Set correlation ID in NLog's scope context (supports async operations)
            using var _ = ScopeContext.PushProperty(CorrelationIdPropertyName, correlationId);

            // Also add to HttpContext for other middleware/controllers to access
            context.Items[CorrelationIdPropertyName] = correlationId;

            // Add correlation ID to response headers for client tracking
            context.Response.OnStarting(() =>
            {
                context.Response.Headers[CorrelationIdHeaderName] = correlationId;
                return Task.CompletedTask;
            });

            await _next(context);

            // Clean up log4net after request
            log4net.LogicalThreadContext.Properties.Remove(CorrelationIdPropertyName);
        }

        private static bool IsValidCorrelationId(string correlationId)
        {
            if (string.IsNullOrEmpty(correlationId) || correlationId.Length > MaxCorrelationIdLength)
            {
                return false;
            }

            foreach (var c in correlationId)
            {
                if (!char.IsLetterOrDigit(c) && c != '-' && c != '_')
                {
                    return false;
                }
            }

            return true;
        }
    }
}
