using System;
using System.Linq;
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

        [Test]
        public void ValidateCertificateWithEmailDisposesTheLoadedCertificate()
        {
            var validator = new CertValidator();
            var cert = CreateCertificate(DateTime.UtcNow.AddYears(-1), DateTime.UtcNow.AddYears(1), "same-cn");
            // ValidateCertificate compares the cert's simple name against email with "@" stripped,
            // so a leading "@" round-trips back to the simple name.
            var email = "@" + cert.GetNameInfo(X509NameType.SimpleName, true);

            var trackingCert = new DisposeTrackingCertificate(cert.RawData);
            var previousLoader = CertValidator.LoadCertificate;
            CertValidator.LoadCertificate = _ => trackingCert;
            try
            {
                validator.ValidateCertificate(Convert.ToBase64String(cert.RawData), email);
            }
            finally
            {
                CertValidator.LoadCertificate = previousLoader;
            }

            // ValidateCertificate loads a certificate that nothing else owns; it must dispose it
            // itself instead of leaking the native handle.
            Assert.That(trackingCert.WasDisposed, Is.True);
        }

        [Test]
        public void ValidateCertificateWithOldCertificateDisposesBothLoadedCertificates()
        {
            var validator = new CertValidator();
            var now = DateTime.UtcNow;
            var newCert = CreateCertificate(now.AddYears(-1), now.AddYears(1), "same-cn");
            var oldCert = CreateCertificate(now.AddYears(-1), now.AddYears(1), "same-cn");

            var trackingNewCert = new DisposeTrackingCertificate(newCert.RawData);
            var trackingOldCert = new DisposeTrackingCertificate(oldCert.RawData);
            var newCertBytes = newCert.RawData;
            var previousLoader = CertValidator.LoadCertificate;
            CertValidator.LoadCertificate = rawData =>
                rawData.SequenceEqual(newCertBytes) ? trackingNewCert : trackingOldCert;
            try
            {
                var newCertificateDto = new CertificateUpdateDto
                {
                    PublicCert = Convert.ToBase64String(newCert.RawData),
                    ParentCertThumbprint = oldCert.Thumbprint,
                    ParentCertHashSignature = "unused",
                    DeviceId = "unused",
                };
                var oldCertificateDb = new CertificateDb { PublicCert = Convert.ToBase64String(oldCert.RawData) };

                validator.ValidateCertificate(newCertificateDto, oldCertificateDb);
            }
            finally
            {
                CertValidator.LoadCertificate = previousLoader;
            }

            // Both the new and the old certificate loaded here are owned by nothing else; they
            // must be disposed instead of leaking their native handles.
            Assert.That(trackingNewCert.WasDisposed, Is.True);
            Assert.That(trackingOldCert.WasDisposed, Is.True);
        }

        private static X509Certificate2 CreateCertificate(DateTime notBefore, DateTime notAfter, string commonName)
        {
            using var rsa = RSA.Create();
            var request = new CertificateRequest($"cn={commonName}", rsa, HashAlgorithmName.SHA512, RSASignaturePadding.Pkcs1);
            return request.CreateSelfSigned(notBefore, notAfter);
        }

        // Lets the test observe whether CertValidator disposes the certificate it loaded.
        private class DisposeTrackingCertificate : X509Certificate2
        {
            public bool WasDisposed { get; private set; }

            public DisposeTrackingCertificate(byte[] rawData) : base(rawData)
            {
            }

            protected override void Dispose(bool disposing)
            {
                WasDisposed = true;
                base.Dispose(disposing);
            }
        }
    }
}
