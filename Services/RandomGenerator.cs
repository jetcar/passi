using System;
using GoogleTracer;

namespace Services
{
    [Profile]
    public class RandomGenerator : IRandomGenerator
    {
        private Random _random;

        public RandomGenerator()
        {
            _random = new Random();
        }

        public string GetNumbersString(int i)
        {
            var from = (int)Math.Pow(10, i - 1);
            var to = (int)Math.Pow(10, i) - 1;
            var result = _random.Next(@from, to);
            return result.ToString();
        }
    }
}