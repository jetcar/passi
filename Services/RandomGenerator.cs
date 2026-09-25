using System;
using System.Security.Cryptography;
using GoogleTracer;

namespace Services
{
    [Profile]
    public class RandomGenerator : IRandomGenerator
    {
        public string GetNumbersString(int i)
        {
            var from = (int)Math.Pow(10, i - 1);
            var to = (int)Math.Pow(10, i) - 1;
            var result = RandomNumberGenerator.GetInt32(@from, to);
            return result.ToString();
        }
    }
}