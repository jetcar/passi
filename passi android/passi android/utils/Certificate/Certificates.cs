using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Operators;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Utilities;
using Org.BouncyCastle.X509;

namespace passi_android.utils.Certificate
{
    public class CertificatesService : ICertificatesService
    {
        public async Task<Tuple<X509Certificate2, string, byte[]>> GenerateCertificate(string subject, MySecureString pin)
        {
            var random = new SecureRandom();
            var certificateGenerator = new X509V3CertificateGenerator();

            var serialNumber = BigIntegers.CreateRandomInRange(BigInteger.One, BigInteger.ValueOf(Int64.MaxValue), random);
            certificateGenerator.SetSerialNumber(serialNumber);

            var sha512 = subject;
            certificateGenerator.SetIssuerDN(new X509Name($"C=NL, O=Passi, CN={sha512}"));
            certificateGenerator.SetSubjectDN(new X509Name($"C=NL, O=Passi, CN={sha512}"));
            certificateGenerator.SetNotBefore(DateTime.UtcNow.Date);
            certificateGenerator.SetNotAfter(DateTime.UtcNow.Date.AddYears(1));

            const int strength = 2048;
            var keyGenerationParameters = new KeyGenerationParameters(random, strength);
            var keyPairGenerator = new RsaKeyPairGenerator();
            keyPairGenerator.Init(keyGenerationParameters);

            var subjectKeyPair = keyPairGenerator.GenerateKeyPair();
            certificateGenerator.SetPublicKey(subjectKeyPair.Public);

            var issuerKeyPair = subjectKeyPair;
            const string signatureAlgorithm = "SHA256WithRSA";
            var signatureFactory = new Asn1SignatureFactory(signatureAlgorithm, issuerKeyPair.Private);
            var bouncyCert = certificateGenerator.Generate(signatureFactory);

            // Lets convert it to X509Certificate2
            X509Certificate2 certificate;

            Pkcs12Store store = new Pkcs12StoreBuilder().Build();
            store.SetKeyEntry($"{sha512}_key", new AsymmetricKeyEntry(subjectKeyPair.Private), new[] { new X509CertificateEntry(bouncyCert) });
            MySecureString password = new MySecureString(Guid.NewGuid().ToString());
            MySecureString fullPassword = new MySecureString(password).Append(pin);

            using (var ms = new System.IO.MemoryStream())
            {
                store.Save(ms, fullPassword.SecureStringToString().ToCharArray(), random);
                var rawBytes = ms.ToArray();
                certificate = new X509Certificate2(rawBytes, fullPassword.SecureStringToString(), X509KeyStorageFlags.Exportable);
                var result = new Tuple<X509Certificate2, string, byte[]>(certificate, password.SecureStringToString(), rawBytes);
                return result;
            }
        }
    }

    public interface ICertificatesService
    {
        Task<Tuple<X509Certificate2, string, byte[]>> GenerateCertificate(string subject, MySecureString pin);
    }

    public static class SecureStringExtensions
    {
        public static SecureString AppendChars(this SecureString str, SecureString chars)
        {
            foreach (var charValue in chars.SecureStringToString())
            {
                str.AppendChar(charValue);
            }
            return str;
        }

        public static string SecureStringToString(this SecureString value) {
            IntPtr valuePtr = IntPtr.Zero;
            try {
                valuePtr = Marshal.SecureStringToGlobalAllocUnicode(value);
                return Marshal.PtrToStringUni(valuePtr);
            } finally {
                Marshal.ZeroFreeGlobalAllocUnicode(valuePtr);
            }
        }
    }


}