using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Models;
using NUnit.Framework;
using Services;
using WebApiDto.Certificate;

namespace ServicesTests
{
    public class CertValidatorTests
    {
        [Test]
        public void ValidateCertificateRejectsANewCertificateThatExpiredEarlierToday()
        {
            var validator = new CertValidator();
            var now = DateTime.UtcNow;

            // Already expired a minute ago, but NotAfter still falls on today's calendar date, so
            // a check against DateTime.UtcNow.Date (midnight) wrongly treats it as still valid.
            var expiredCert = CreateCertificate(now.AddYears(-1), now.AddMinutes(-1), "same-cn");
            var oldCert = CreateCertificate(now.AddYears(-1), now.AddYears(1), "same-cn");

            var newCertificateDto = new CertificateUpdateDto
            {
                PublicCert = Convert.ToBase64String(expiredCert.RawData),
                ParentCertThumbprint = oldCert.Thumbprint,
                ParentCertHashSignature = "unused",
                DeviceId = "unused",
            };
            var oldCertificateDb = new CertificateDb { PublicCert = Convert.ToBase64String(oldCert.RawData) };

            Assert.Throws<BadRequestException>(() => validator.ValidateCertificate(newCertificateDto, oldCertificateDb));
        }

        [Test]
        public void ValidateCertificateAcceptsANewCertificateThatBecameValidEarlierToday()
        {
            var validator = new CertValidator();
            var now = DateTime.UtcNow;

            // Became valid a minute ago (NotBefore is in the past relative to "now"), but NotBefore
            // still falls on today's calendar date, so a check against DateTime.UtcNow.Date wrongly
            // rejects it as "not started" until tomorrow.
            var validCert = CreateCertificate(now.AddMinutes(-1), now.AddYears(1), "same-cn");
            var oldCert = CreateCertificate(now.AddYears(-1), now.AddYears(1), "same-cn");

            var newCertificateDto = new CertificateUpdateDto
            {
                PublicCert = Convert.ToBase64String(validCert.RawData),
                ParentCertThumbprint = oldCert.Thumbprint,
                ParentCertHashSignature = "unused",
                DeviceId = "unused",
            };
            var oldCertificateDb = new CertificateDb { PublicCert = Convert.ToBase64String(oldCert.RawData) };

            Assert.DoesNotThrow(() => validator.ValidateCertificate(newCertificateDto, oldCertificateDb));
        }

        private static X509Certificate2 CreateCertificate(DateTime notBefore, DateTime notAfter, string commonName)
        {
            using var rsa = RSA.Create();
            var request = new CertificateRequest($"cn={commonName}", rsa, HashAlgorithmName.SHA512, RSASignaturePadding.Pkcs1);
            return request.CreateSelfSigned(notBefore, notAfter);
        }
    }
}
