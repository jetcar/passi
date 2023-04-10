﻿using System;
using System.Security;
using System.Security.Cryptography.X509Certificates;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Operators;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Utilities;
using Org.BouncyCastle.X509;

namespace passi_maui.utils.Certificate
{
    public static class Certificates
    {
        public static async Task<Tuple<X509Certificate2, string, byte[]>> GenerateCertificate(string subject, string pin)
        {
            var random = new SecureRandom();
            var certificateGenerator = new X509V3CertificateGenerator();

            var serialNumber = BigIntegers.CreateRandomInRange(BigInteger.One, BigInteger.ValueOf(Int64.MaxValue), random);
            certificateGenerator.SetSerialNumber(serialNumber);

            var username = subject;
            certificateGenerator.SetIssuerDN(new X509Name($"C=NL, O=Passi, CN={username}"));
            certificateGenerator.SetSubjectDN(new X509Name($"C=NL, O=Passi, CN={username}"));
            certificateGenerator.SetNotBefore(DateTime.UtcNow.Date);
            certificateGenerator.SetNotAfter(DateTime.UtcNow.Date.AddYears(1));

            const int strength = 2048;
            var keyGenerationParameters = new KeyGenerationParameters(random, strength);
            var keyPairGenerator = new RsaKeyPairGenerator();
            keyPairGenerator.Init(keyGenerationParameters);

            var subjectKeyPair = keyPairGenerator.GenerateKeyPair();
            certificateGenerator.SetPublicKey(subjectKeyPair.Public);

            var issuerKeyPair = subjectKeyPair;
            const string signatureAlgorithm = "SHA512WithRSA";
            var signatureFactory = new Asn1SignatureFactory(signatureAlgorithm, issuerKeyPair.Private);
            var bouncyCert = certificateGenerator.Generate(signatureFactory);

            // Lets convert it to X509Certificate2

            Pkcs12Store store = new Pkcs12StoreBuilder().Build();
            store.SetKeyEntry($"{username}_key", new AsymmetricKeyEntry(subjectKeyPair.Private), new[] { new X509CertificateEntry(bouncyCert) });

            var password = Guid.NewGuid().ToString();
            var charArray = (password + pin).ToCharArray();
            var fullPassword = (charArray);

            using (var ms = new System.IO.MemoryStream())
            {
                store.Save(ms, fullPassword.ToArray(), random);
                var bytes = ms.ToArray();
                var base64 = Convert.ToBase64String(bytes);
                var certificate = new X509Certificate2(bytes, fullPassword, X509KeyStorageFlags.DefaultKeySet);
                //certificate.CopyWithPrivateKey(key)
                var result = new Tuple<X509Certificate2, string, byte[]>(certificate, password, bytes);
                return result;
            }
        }
    }
}