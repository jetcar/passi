using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using NUnit.Framework;
using Services;

namespace ServicesTests
{
    public class RandomGeneratorTests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void Test1()
        {
            var randomGenerator = new RandomGenerator();
            for (int i = 1; i <= 10; i++)
            {
                for (int j = 0; j < 10; j++)
                {
                    var str = randomGenerator.GetNumbersString(i);
                    Assert.That(i == str.Length);
                }
            }
        }

        [Test]
        public void GetNumbersStringIsThreadSafeUnderConcurrentAccess()
        {
            // RandomGenerator is registered as a DI singleton, so a single instance is
            // shared across concurrent requests. This reproduces that with a thread pool
            // hammering the same instance; a non-thread-safe generator (e.g. a shared
            // System.Random) corrupts its internal state under this load and either
            // throws or returns a value outside the requested digit range.
            var randomGenerator = new RandomGenerator();
            var exceptions = new ConcurrentBag<Exception>();

            Parallel.For(0, 2000, _ =>
            {
                try
                {
                    var str = randomGenerator.GetNumbersString(6);
                    if (str.Length != 6)
                        throw new InvalidOperationException($"Expected a 6-digit code but got '{str}'");
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            });

            Assert.That(exceptions, Is.Empty, () => string.Join(Environment.NewLine, exceptions));
        }
    }
}