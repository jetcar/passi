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
        private ICertificateRepository _certificateRepository;

        public CertificatesService(ICertificateRepository certificateRepository)
        {
            _certificateRepository = certificateRepository;
        }

        public CertificateDb UpdateCertificate(CertificateUpdateDto newCertificate)
        {
            var certificate = new X509Certificate2(Convert.FromBase64String(newCertificate.PublicCert));
            return _certificateRepository.AddCertificate(certificate.Thumbprint, newCertificate.PublicCert,
                newCertificate.ParentCertThumbprint, newCertificate.DeviceId);
        }
    }

    public interface ICertificatesService
    {
        CertificateDb UpdateCertificate(CertificateUpdateDto newCertificate);
    }
}