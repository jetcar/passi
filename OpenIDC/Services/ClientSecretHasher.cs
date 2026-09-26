using System;
using System.Security.Cryptography;
using System.Text;

namespace OpenIDC.Services
{
    /// <summary>
    /// Client secrets are 256-bit random values, so a single SHA-256 is sufficient (no slow KDF needed);
    /// the plaintext is only ever shown to the owner once.
    /// </summary>
    public static class ClientSecretHasher
    {
        public static string GenerateSecret() => ToBase64Url(RandomNumberGenerator.GetBytes(32));

        public static string GenerateClientId() => "pc_" + ToBase64Url(RandomNumberGenerator.GetBytes(18));

        public static string Hash(string secret) =>
            Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(secret))).ToLowerInvariant();

        public static bool Verify(string secret, string expectedHash)
        {
            if (string.IsNullOrEmpty(secret) || string.IsNullOrEmpty(expectedHash))
                return false;

            return CryptographicOperations.FixedTimeEquals(
                Encoding.ASCII.GetBytes(Hash(secret)),
                Encoding.ASCII.GetBytes(expectedHash));
        }

        private static string ToBase64Url(byte[] bytes) =>
            Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }
}
