using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using AppCommon;
using Models;
using WebApiDto.Certificate;

namespace Services
{
    public class CertValidator : ICertValidator
    {
        public void ValidateCertificate(string publicCertBase64, string email)
        {
            var fromBase64String = Convert.FromBase64String(publicCertBase64);
            var publicCert = new X509Certificate2(fromBase64String);

            if (publicCert == null)
                throw new BadRequestException("Certificate is missing");
            if (publicCert.NotAfter < DateTime.UtcNow || publicCert.NotBefore > DateTime.UtcNow)
                throw new BadRequestException("Certificate is expired");
            if (publicCert.GetNameInfo(X509NameType.SimpleName, true) != email.ToSha512())
                throw new BadRequestException("Certificate Email do not match");
        }

        public void ValidateCertificate(CertificateDto newPublicCertDto, CertificateDb oldPublicCertDb)
        {
            var fromBase64String = Convert.FromBase64String(newPublicCertDto.PublicCert);
            var newPublicCert = new X509Certificate2(fromBase64String);

            var oldBase64String = Convert.FromBase64String(oldPublicCertDb.PublicCert);
            var oldpublicCert = new X509Certificate2(oldBase64String);

            if (newPublicCert == null)
                throw new BadRequestException("Certificate is missing");
            if (newPublicCert.NotAfter < DateTime.UtcNow.Date)
                throw new BadRequestException("Certificate is expired");
            if (newPublicCert.NotBefore > DateTime.UtcNow.Date)
                throw new BadRequestException("Certificate is not started");
            if (newPublicCert.GetNameInfo(X509NameType.SimpleName, true) !=
                oldpublicCert.GetNameInfo(X509NameType.SimpleName, true))
                throw new BadRequestException("Certificate Email do not match");
        }

        public void ValidateCertificateChain(CertificateDto newCertificate, CertificateDb oldCertificate)
        {
            var verify = CertHelper.VerifyData(newCertificate.PublicCert, newCertificate.ParentCertHashSignature,
                oldCertificate.PublicCert);

            if (!verify)
            {
                throw new BadRequestException("invalid signature or parentCertificate");
            }
        }
    }

    public interface ICertValidator
    {
        void ValidateCertificate(string publicCertBase64, string email);

        void ValidateCertificate(CertificateDto newPublicCertDto, CertificateDb oldPublicCertDb);

        void ValidateCertificateChain(CertificateDto newCertificate, CertificateDb oldCertificate);
    }
}