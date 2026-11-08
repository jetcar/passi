using System;
using System.Security.Cryptography;
using System.Text;
using System.Linq;

namespace OpenIDC.Helpers
{
    public static class PkceHelper
    {
        public static bool ValidateCodeChallenge(string codeVerifier, string codeChallenge, string codeChallengeMethod)
        {
            if (codeVerifier == null || codeVerifier.Length is < 43 or > 128 ||
                codeVerifier.Any(c => !IsUnreserved(c)) || !IsS256Challenge(codeChallenge, codeChallengeMethod))
            {
                return false;
            }

            if (codeChallengeMethod == "S256")
            {
                using var sha256 = SHA256.Create();
                var hash = sha256.ComputeHash(Encoding.ASCII.GetBytes(codeVerifier));
                var computedChallenge = Base64UrlEncode(hash);
                return FixedTimeEquals(computedChallenge, codeChallenge);
            }

            return false;
        }

        public static bool IsS256Challenge(string value, string method) =>
            method == "S256" && value?.Length == 43 && value.All(c =>
                char.IsAsciiLetterOrDigit(c) || c == '-' || c == '_');

        private static bool IsUnreserved(char c) => char.IsAsciiLetterOrDigit(c) || c is '-' or '.' or '_' or '~';

        private static string Base64UrlEncode(byte[] input)
        {
            var base64 = Convert.ToBase64String(input);
            return base64.Replace("+", "-").Replace("/", "_").Replace("=", "");
        }

        private static bool FixedTimeEquals(string a, string b)
        {
            if (a == null || b == null)
                return false;

            return CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(a),
                Encoding.UTF8.GetBytes(b));
        }
    }
}
