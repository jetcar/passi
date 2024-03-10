// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using IdentityServer4.Models;
using IdentityServer4.Stores;
using IdentityServer4.Stores.InMemory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

using System;
using System.Linq;
using System.Security.Cryptography.X509Certificates;

namespace IdentityServer4.Configuration.DependencyInjection.BuilderExtensions
{
    /// <summary>
    /// Builder extension methods for registering crypto services
    /// </summary>
    [GoogleTracer.Profile]
    public static class IdentityServerBuilderExtensionsCrypto
    {
        /// <summary>
        /// Sets the signing credential.
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <param name="credential">The credential.</param>
        /// <returns></returns>
        public static IIdentityServerBuilder AddSigningCredential(this IIdentityServerBuilder builder, SigningCredentials credential)
        {
            if (!(credential.Key is AsymmetricSecurityKey
                || credential.Key is Microsoft.IdentityModel.Tokens.JsonWebKey && ((Microsoft.IdentityModel.Tokens.JsonWebKey)credential.Key).HasPrivateKey))
            {
                throw new InvalidOperationException("Signing key is not asymmetric");
            }

            if (!IdentityServerConstants.SupportedSigningAlgorithms.Contains(credential.Algorithm, StringComparer.Ordinal))
            {
                throw new InvalidOperationException($"Signing algorithm {credential.Algorithm} is not supported.");
            }

            if (credential.Key is ECDsaSecurityKey key && !CryptoHelper.IsValidCurveForAlgorithm(key, credential.Algorithm))
            {
                throw new InvalidOperationException("Invalid curve for signing algorithm");
            }

            if (credential.Key is Microsoft.IdentityModel.Tokens.JsonWebKey jsonWebKey)
            {
                if (jsonWebKey.Kty == JsonWebAlgorithmsKeyTypes.EllipticCurve && !CryptoHelper.IsValidCrvValueForAlgorithm(jsonWebKey.Crv))
                    throw new InvalidOperationException("Invalid crv value for signing algorithm");
            }

            builder.Services.AddSingleton<ISigningCredentialStore>(new InMemorySigningCredentialsStore(credential));

            var keyInfo = new SecurityKeyInfo
            {
                Key = credential.Key,
                SigningAlgorithm = credential.Algorithm
            };

            builder.Services.AddSingleton<IValidationKeysStore>(new InMemoryValidationKeysStore(new[] { keyInfo }));

            return builder;
        }

        /// <summary>
        /// Sets the signing credential.
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <param name="certificate">The certificate.</param>
        /// <param name="signingAlgorithm">The signing algorithm (defaults to RS256)</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="InvalidOperationException">X509 certificate does not have a private key.</exception>
        public static IIdentityServerBuilder AddSigningCredential(this IIdentityServerBuilder builder, X509Certificate2 certificate, string signingAlgorithm = SecurityAlgorithms.RsaSha256)
        {
            if (certificate == null) throw new ArgumentNullException(nameof(certificate));

            if (!certificate.HasPrivateKey)
            {
                throw new InvalidOperationException("X509 certificate does not have a private key.");
            }

            // add signing algorithm name to key ID to allow using the same key for two different algorithms (e.g. RS256 and PS56);
            var key = new X509SecurityKey(certificate);
            key.KeyId += signingAlgorithm;

            var credential = new SigningCredentials(key, signingAlgorithm);
            return builder.AddSigningCredential(credential);
        }
    }
}