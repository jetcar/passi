using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Models;
using NUnit.Framework;
using Repos;
using Services;
using WebApiDto.Certificate;

namespace ServicesTests
{
    public class CertificatesServiceTests
    {
        [Test]
        public void UpdateCertificateDisposesTheLoadedCertificate()
        {
            var cert = CreateCertificate(DateTime.UtcNow.AddYears(-1), DateTime.UtcNow.AddYears(1), "same-cn");
            var trackingCert = new DisposeTrackingCertificate(cert.RawData);
            var previousLoader = CertificatesService.LoadCertificate;
            CertificatesService.LoadCertificate = _ => trackingCert;

            var service = new CertificatesService(new FakeCertificateRepository());
            var newCertificate = new CertificateUpdateDto
            {
                PublicCert = Convert.ToBase64String(cert.RawData),
                ParentCertThumbprint = "parent-thumbprint",
                ParentCertHashSignature = "unused",
                DeviceId = "device-1",
            };

            try
            {
                service.UpdateCertificate(newCertificate);
            }
            finally
            {
                CertificatesService.LoadCertificate = previousLoader;
            }

            // UpdateCertificate loads a certificate that nothing else owns; it must dispose it
            // itself instead of leaking the native handle.
            Assert.That(trackingCert.WasDisposed, Is.True);
        }

        private static X509Certificate2 CreateCertificate(DateTime notBefore, DateTime notAfter, string commonName)
        {
            using var rsa = RSA.Create();
            var request = new CertificateRequest($"cn={commonName}", rsa, HashAlgorithmName.SHA512, RSASignaturePadding.Pkcs1);
            return request.CreateSelfSigned(notBefore, notAfter);
        }

        // Lets the test observe whether CertificatesService disposes the certificate it loaded.
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

        private class FakeCertificateRepository : ICertificateRepository
        {
            public CertificateDb GetUserCertificate(string username, string thumbprint) => null;

            public CertificateDb AddCertificate(string certificateThumbprint, string publicCert,
                string parentCertThumbprint, string deviceId) =>
                new CertificateDb { Thumbprint = certificateThumbprint, PublicCert = publicCert };

            public CertificateDb GetCertificate(string parentCertThumbprint) => null;

            public IDbContextTransaction BeginTransaction() => null;

            public IExecutionStrategy GetExecutionStrategy() => null;
        }
    }
}
