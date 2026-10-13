using System.Threading.Tasks;
using GoogleTracer;
using NUnit.Framework;

namespace GoogleTracerTests
{
    [TestFixture]
    public class TracerCurrentTracerConcurrencyTests
    {
        [Test]
        public async Task CurrentTracerIsIsolatedBetweenConcurrentRequests()
        {
            var tracerForRequestA = new FakeManagedTracer();
            var tracerForRequestB = new FakeManagedTracer();

            IManagedTracerHolder observedByRequestA = new IManagedTracerHolder();

            var requestA = Task.Run(async () =>
            {
                Tracer.CurrentTracer = tracerForRequestA;
                // Give request B a chance to run (and overwrite a shared static)
                // before request A reads the value back.
                await Task.Delay(150);
                observedByRequestA.Tracer = Tracer.CurrentTracer;
            });

            var requestB = Task.Run(async () =>
            {
                await Task.Delay(30);
                Tracer.CurrentTracer = tracerForRequestB;
            });

            await Task.WhenAll(requestA, requestB);

            Assert.That(
                observedByRequestA.Tracer,
                Is.SameAs(tracerForRequestA),
                "Request A's own tracer should still be visible to request A after request B " +
                "set its own tracer concurrently; seeing request B's tracer means spans are " +
                "leaking across concurrent requests.");
        }

        private class IManagedTracerHolder
        {
            public Google.Cloud.Diagnostics.Common.IManagedTracer Tracer;
        }
    }
}
