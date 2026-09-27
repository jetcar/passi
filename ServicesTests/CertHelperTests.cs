using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using NUnit.Framework;
using Services;

namespace ServicesTests
{
    public class CertHelperTests
    {
        [Test]
        public void VerifyDataAcceptsASignatureProducedByTheMatchingPrivateKey()
        {
            var cert = CreateCertificate();

            var signedData = Sign("some-challenge", cert);

            var result = CertHelper.VerifyData("some-challenge", signedData, PublicCertBase64(cert));

            Assert.That(result, Is.True);
        }

        [Test]
        public void VerifyDataRejectsASignatureOverDifferentData()
        {
            var cert = CreateCertificate();

            // Signature was produced over different data than what is being verified.
            var signedData = Sign("original-challenge", cert);

            var result = CertHelper.VerifyData("tampered-challenge", signedData, PublicCertBase64(cert));

            Assert.That(result, Is.False);
        }

        [Test]
        public void VerifyDataRejectsASignatureFromAnUnrelatedKey()
        {
            var signingCert = CreateCertificate();
            var otherCert = CreateCertificate();

            var signedData = Sign("some-challenge", signingCert);

            // Verifying against a certificate whose private key never produced this signature.
            var result = CertHelper.VerifyData("some-challenge", signedData, PublicCertBase64(otherCert));

            Assert.That(result, Is.False);
        }

        [Test]
        public void VerifyDataThrowsOnNullArguments()
        {
            var cert = CreateCertificate();
            var signedData = Sign("some-challenge", cert);
            var publicCert = PublicCertBase64(cert);

            Assert.Throws<ArgumentNullException>(() => CertHelper.VerifyData(null, signedData, publicCert));
            Assert.Throws<ArgumentNullException>(() => CertHelper.VerifyData("some-challenge", null, publicCert));
            Assert.Throws<ArgumentNullException>(() => CertHelper.VerifyData("some-challenge", signedData, null));
        }

        private static string Sign(string data, X509Certificate2 certificate)
        {
            using var sha512 = SHA512.Create();
            var hash = sha512.ComputeHash(Encoding.ASCII.GetBytes(data));
            var signedBytes = certificate.GetRSAPrivateKey().SignHash(hash, HashAlgorithmName.SHA512, RSASignaturePadding.Pkcs1);
            return Convert.ToBase64String(signedBytes);
        }

        private static string PublicCertBase64(X509Certificate2 certificate)
        {
            return Convert.ToBase64String(certificate.RawData);
        }

        private static X509Certificate2 CreateCertificate()
        {
            using var rsa = RSA.Create();
            var request = new CertificateRequest($"cn={Guid.NewGuid()}", rsa, HashAlgorithmName.SHA512, RSASignaturePadding.Pkcs1);
            return request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddYears(1));
        }
    }
}
