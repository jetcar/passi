using IdentityModel;
using Microsoft.IdentityModel.Tokens;

using System;
using System.Security.Cryptography;
using System.Text;

namespace IdentityServer4.Configuration
{
    /// <summary>
    /// Crypto helper
    /// </summary>
    [GoogleTracer.Profile]
    public static class CryptoHelper
    {
        /// <summary>
        /// Creates the hash for the various hash claims (e.g. c_hash, at_hash or s_hash).
        /// </summary>
        /// <param name="value">The value to hash.</param>
        /// <param name="tokenSigningAlgorithm">The token signing algorithm</param>
        /// <returns></returns>
        public static string CreateHashClaimValue(string value, string tokenSigningAlgorithm)
        {
            using (var sha = GetHashAlgorithmForSigningAlgorithm(tokenSigningAlgorithm))
            {
                var hash = sha.ComputeHash(Encoding.ASCII.GetBytes(value));
                var size = (sha.HashSize / 8) / 2;

                var leftPart = new byte[size];
                Array.Copy(hash, leftPart, size);

                return Base64Url.Encode(leftPart);
            }
        }

        /// <summary>
        /// Returns the matching hashing algorithm for a token signing algorithm
        /// </summary>
        /// <param name="signingAlgorithm">The signing algorithm</param>
        /// <returns></returns>
        public static HashAlgorithm GetHashAlgorithmForSigningAlgorithm(string signingAlgorithm)
        {
            var signingAlgorithmBits = int.Parse(signingAlgorithm.Substring(signingAlgorithm.Length - 3));

            return signingAlgorithmBits switch
            {
                256 => SHA256.Create(),
                384 => SHA384.Create(),
                512 => SHA512.Create(),
                _ => throw new InvalidOperationException($"Invalid signing algorithm: {signingAlgorithm}"),
            };
        }

        /// <summary>
        /// Return the matching RFC 7518 crv value for curve
        /// </summary>
        internal static string GetCrvValueFromCurve(ECCurve curve)
        {
            return curve.Oid.Value switch
            {
                Constants.CurveOids.P256 => JsonWebKeyECTypes.P256,
                Constants.CurveOids.P384 => JsonWebKeyECTypes.P384,
                Constants.CurveOids.P521 => JsonWebKeyECTypes.P521,
                _ => throw new InvalidOperationException($"Unsupported curve type of {curve.Oid.Value} - {curve.Oid.FriendlyName}"),
            };
        }

        internal static bool IsValidCurveForAlgorithm(ECDsaSecurityKey key, string algorithm)
        {
            var parameters = key.ECDsa.ExportParameters(false);

            if (algorithm == SecurityAlgorithms.EcdsaSha256 && parameters.Curve.Oid.Value != Constants.CurveOids.P256
                || algorithm == SecurityAlgorithms.EcdsaSha384 && parameters.Curve.Oid.Value != Constants.CurveOids.P384
                || algorithm == SecurityAlgorithms.EcdsaSha512 && parameters.Curve.Oid.Value != Constants.CurveOids.P521)
            {
                return false;
            }

            return true;
        }

        internal static bool IsValidCrvValueForAlgorithm(string crv)
        {
            return crv == JsonWebKeyECTypes.P256 ||
                   crv == JsonWebKeyECTypes.P384 ||
                   crv == JsonWebKeyECTypes.P521;
        }
    }
}