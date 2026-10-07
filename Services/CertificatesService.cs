using Models;

using Repos;
using System;
using System.Security.Cryptography.X509Certificates;
using GoogleTracer;
using WebApiDto.Certificate;

namespace Services
{
    [Profile]
    public class CertificatesService : ICertificatesService
    {
        // Test seam: lets tests observe that the certificate loaded here is disposed, without
        // changing UpdateCertificate's public contract (callers still just pass a DTO).
        internal static Func<byte[], X509Certificate2> LoadCertificate = X509CertificateLoader.LoadCertificate;

        private ICertificateRepository _certificateRepository;

        public CertificatesService(ICertificateRepository certificateRepository)
        {
            _certificateRepository = certificateRepository;
        }

        public CertificateDb UpdateCertificate(CertificateUpdateDto newCertificate)
        {
            using var certificate = LoadCertificate(Convert.FromBase64String(newCertificate.PublicCert));
            return _certificateRepository.AddCertificate(certificate.Thumbprint, newCertificate.PublicCert,
                newCertificate.ParentCertThumbprint, newCertificate.DeviceId);
        }
    }

    public interface ICertificatesService
    {
        CertificateDb UpdateCertificate(CertificateUpdateDto newCertificate);
    }
}