using System.Collections.Concurrent;
using System.Threading;
using NUnit.Framework;

namespace PassiWebApiTests;

public class SharedTestResourceConcurrencyTests
{
    // Barrier releases both callers at the same instant, as two [OneTimeSetUp] fixtures running in
    // parallel would both enter PrepareDockers() around the same time; the factory's own delay (standing
    // in for a real Testcontainers StartAsync()) then widens the check-then-act window so an
    // unsynchronized GetOrCreate() reliably lets both callers observe "not created yet" before either
    // one finishes assigning.
    [Test]
    public void ConcurrentGetOrCreateCallsBuildExactlyOneInstance()
    {
        var barrier = new Barrier(2);
        var factoryCalls = 0;
        var created = new ConcurrentBag<object>();
        var resource = new SharedTestResource<object>(() =>
        {
            Interlocked.Increment(ref factoryCalls);
            Thread.Sleep(200);
            var instance = new object();
            created.Add(instance);
            return instance;
        });

        var results = new object[2];
        var t1 = new Thread(() => { barrier.SignalAndWait(); results[0] = resource.GetOrCreate(); });
        var t2 = new Thread(() => { barrier.SignalAndWait(); results[1] = resource.GetOrCreate(); });
        t1.Start();
        t2.Start();
        t1.Join();
        t2.Join();

        Assert.That(factoryCalls, Is.EqualTo(1),
            "the factory should run exactly once; running it twice means two containers were started " +
            "for what should be a single shared instance");
        Assert.That(results[0], Is.SameAs(results[1]),
            "every caller should observe the same shared instance");
    }
}
