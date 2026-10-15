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
            // Building the i-digit range as 10^(i-1)..10^i overflows Int32 once i reaches 10
            // (10^10 > int.MaxValue), silently truncating the usable range. Drawing each digit
            // independently avoids that overflow for any i.
            var digits = new char[i];
            digits[0] = (char)('0' + RandomNumberGenerator.GetInt32(1, 10));
            for (var index = 1; index < i; index++)
            {
                digits[index] = (char)('0' + RandomNumberGenerator.GetInt32(0, 10));
            }
            return new string(digits);
        }
    }
}