using System;
using static Abstracta.JmeterDsl.JmeterDsl;

namespace LoadTests
{
    public class Tests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void Test1()
        {
            var stats = TestPlan(
                ThreadGroup(2, 10,
                    HttpSampler("https://localhost")
                )
            ).Run();
            Assert.That(stats.Overall.SampleTimePercentile99, Is.LessThan(TimeSpan.FromSeconds(5)));
        }
    }
}