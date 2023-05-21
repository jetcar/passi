using System;
using System.Security.Cryptography;
using System.Text;

namespace AppCommon
{
    public static class StringExtensions
    {
        public const string characters = "qwertyuiopasdfghjklzxcvbnm1234567890";

        public static string ToSha512(this string input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;

            using (var sha = SHA512.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(input);
                var hash = sha.ComputeHash(bytes);

                var base64String = Convert.ToBase64String(hash);

                return FilterOutSymbols(base64String);
            }
        }

        public static long ToTimestamp(this DateTime value)
        {
            long epoch = (value.Ticks - 621355968000000000) / 10000000;
            return epoch;
        }

        public static DateTime ToDateTime(this long value)
        {
            var epoch = new DateTime(value * 10000000 + 621355968000000000);
            return epoch;
        }

        private static string FilterOutSymbols(string base64String)
        {
            var result = new StringBuilder(base64String.Length);
            foreach (var charecter in base64String)
            {
                if (characters.Contains(charecter.ToString()))
                    result.Append(charecter);
            }

            return result.ToString();
        }

        public static string Truncate(this string text, int maxLength)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            // If we're asked for more than we've got, we can just return the
            // original reference
            return text.Length > maxLength ? text.Substring(0, maxLength) : text;
        }
    }
}