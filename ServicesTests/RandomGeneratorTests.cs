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

        [Test]
        public void GetNumbersStringCanProduceTheLargestValueOfTheRequestedDigitCount()
        {
            // GetNumbersString(1) should be able to return every single digit "0".."9" - in
            // particular "9", the largest. With only 9 possible digits, 2000 draws make it
            // astronomically unlikely (less than 1e-99) to miss "9" by chance alone; a generator
            // that systematically excludes the upper bound (an off-by-one against
            // RandomNumberGenerator.GetInt32's exclusive-upper-bound contract) will never produce
            // it, no matter how many draws are taken.
            var randomGenerator = new RandomGenerator();

            var sawMaxDigit = false;
            for (int j = 0; j < 2000; j++)
            {
                if (randomGenerator.GetNumbersString(1) == "9")
                {
                    sawMaxDigit = true;
                    break;
                }
            }

            Assert.That(sawMaxDigit, Is.True, "expected \"9\" to be producible, but it never appeared in 2000 draws");
        }

        [Test]
        public void GetNumbersStringOfTenDigitsCanProduceAFirstDigitAboveTwo()
        {
            // GetNumbersString(10) builds its range as (int)Math.Pow(10, 9) to (int)Math.Pow(10, 10).
            // 10^10 overflows Int32 and the cast saturates to int.MaxValue (2147483647), so the
            // generator can only ever draw from [1000000000, 2147483647) - every result starts with
            // "1" or "2". A correct generator covering the full 10-digit range makes a first digit of
            // "3".."9" overwhelmingly likely within 2000 draws (~87% per draw), so missing it entirely
            // means the range is still truncated.
            var randomGenerator = new RandomGenerator();

            var sawHighFirstDigit = false;
            for (int j = 0; j < 2000; j++)
            {
                var str = randomGenerator.GetNumbersString(10);
                if (str[0] >= '3')
                {
                    sawHighFirstDigit = true;
                    break;
                }
            }

            Assert.That(sawHighFirstDigit, Is.True,
                "expected a first digit of 3-9 to be producible for a 10-digit string, but it never appeared in 2000 draws");
        }
    }
}