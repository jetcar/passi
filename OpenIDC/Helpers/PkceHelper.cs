using System;
using System.Security.Cryptography;
using System.Text;

namespace OpenIDC.Helpers
{
    public static class PkceHelper
    {
        public static bool ValidateCodeChallenge(string codeVerifier, string codeChallenge, string codeChallengeMethod)
        {
            if (string.IsNullOrEmpty(codeVerifier) || string.IsNullOrEmpty(codeChallenge))
            {
                return false;
            }

            if (codeChallengeMethod == "S256")
            {
                using var sha256 = SHA256.Create();
                var hash = sha256.ComputeHash(Encoding.ASCII.GetBytes(codeVerifier));
                var computedChallenge = Base64UrlEncode(hash);
                return computedChallenge == codeChallenge;
            }
            else if (codeChallengeMethod == "plain")
            {
                return codeVerifier == codeChallenge;
            }

            return false;
        }

        private static string Base64UrlEncode(byte[] input)
        {
            var base64 = Convert.ToBase64String(input);
            return base64.Replace("+", "-").Replace("/", "_").Replace("=", "");
        }
    }
}
