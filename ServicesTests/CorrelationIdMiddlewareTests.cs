using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using NUnit.Framework;
using Services;

namespace ServicesTests
{
    public class CorrelationIdMiddlewareTests
    {
        private const string HeaderName = "X-Correlation-Id";

        [Test]
        public async Task ValidCorrelationIdFromTheRequestIsReused()
        {
            var context = new DefaultHttpContext();
            context.Request.Headers[HeaderName] = "client-request-id-123";

            await InvokeMiddleware(context);

            Assert.That(context.Items["CorrelationId"], Is.EqualTo("client-request-id-123"));
        }

        [Test]
        public async Task MissingCorrelationIdGetsAGeneratedId()
        {
            var context = new DefaultHttpContext();

            await InvokeMiddleware(context);

            Assert.That(context.Items["CorrelationId"], Is.Not.Null.And.Not.Empty);
        }

        [Test]
        public async Task OverlongCorrelationIdIsReplacedRatherThanTrusted()
        {
            var context = new DefaultHttpContext();
            context.Request.Headers[HeaderName] = new string('a', 5000);

            await InvokeMiddleware(context);

            Assert.That(((string)context.Items["CorrelationId"]).Length, Is.LessThan(100));
        }

        [Test]
        public async Task CorrelationIdWithControlCharactersIsReplacedRatherThanTrusted()
        {
            // A client could otherwise inject newlines/control characters into every
            // log line for the request via this header.
            var context = new DefaultHttpContext();
            context.Request.Headers[HeaderName] = "abc\r\nFAKE LOG LINE";

            await InvokeMiddleware(context);

            Assert.That((string)context.Items["CorrelationId"], Does.Not.Contain("\r").And.Not.Contain("\n"));
        }

        private static Task InvokeMiddleware(HttpContext context)
        {
            var middleware = new CorrelationIdMiddleware(_ => Task.CompletedTask);
            return middleware.InvokeAsync(context);
        }
    }
}
