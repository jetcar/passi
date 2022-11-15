using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Models;
using Repos;
using WebApiDto.Certificate;

namespace Services
{
    public class CertificatesService : ICertificatesService
    {
        private ICertificateRepository _certificateRepository;

        public CertificatesService(ICertificateRepository certificateRepository)
        {
            _certificateRepository = certificateRepository;
        }

        public CertificateDb UpdateCertificate(CertificateDto newCertificate)
        {
            var certificate = new X509Certificate2(Convert.FromBase64String(newCertificate.PublicCert));
            return _certificateRepository.AddCertificate(certificate.Thumbprint, newCertificate.PublicCert,
                newCertificate.ParentCertThumbprint,newCertificate.DeviceId);
        }
    }

    public interface ICertificatesService
    {
        CertificateDb UpdateCertificate(CertificateDto newCertificate);
    }
}
