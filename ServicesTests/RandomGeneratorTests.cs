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
    }
}