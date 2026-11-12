using Models;

using System;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using GoogleTracer;
using WebApiDto.Certificate;

namespace Services
{
    [Profile]
    public class CertValidator : ICertValidator
    {
        // Test seam: lets tests observe that certificates loaded here are disposed, without
        // changing the public contract (callers still just pass base64 cert strings/DTOs).
        internal static Func<byte[], X509Certificate2> LoadCertificate = X509CertificateLoader.LoadCertificate;

        public void ValidateCertificate(string publicCertBase64, string email)
        {
            var fromBase64String = Convert.FromBase64String(publicCertBase64);
            using var publicCert = LoadCertificate(fromBase64String);

            if (publicCert == null)
                throw new BadRequestException("Certificate is missing");
            if (publicCert.NotAfter.ToUniversalTime() < DateTime.UtcNow || publicCert.NotBefore.ToUniversalTime() >= DateTime.UtcNow)
                throw new BadRequestException("Certificate is expired");
            var nameInfo = publicCert.GetNameInfo(X509NameType.SimpleName, true);
            if (nameInfo != email.Replace("@", ""))
                throw new BadRequestException("Certificate Email do not match");
        }

        public void ValidateCertificate(CertificateUpdateDto newPublicCertDto, CertificateDb oldPublicCertDb)
        {
            var fromBase64String = Convert.FromBase64String(newPublicCertDto.PublicCert);
            using var newPublicCert = LoadCertificate(fromBase64String);

            var oldBase64String = Convert.FromBase64String(oldPublicCertDb.PublicCert);
            using var oldpublicCert = LoadCertificate(oldBase64String);

            if (newPublicCert == null)
                throw new BadRequestException("Certificate is missing");
            if (newPublicCert.NotAfter.ToUniversalTime() < DateTime.UtcNow)
                throw new BadRequestException("Certificate is expired");
            if (newPublicCert.NotBefore.ToUniversalTime() > DateTime.UtcNow)
                throw new BadRequestException("Certificate is not started");
            if (newPublicCert.GetNameInfo(X509NameType.SimpleName, true) !=
                oldpublicCert.GetNameInfo(X509NameType.SimpleName, true).Replace("@", ""))
                throw new BadRequestException("Certificate Email do not match");
        }

        public void ValidateCertificateChain(CertificateUpdateDto newCertificate, CertificateDb oldCertificate)
        {
            // The unchanged signature primitive consumes ASCII and this envelope uses newlines.
            // Reject ambiguous encodings rather than let different device identifiers sign alike.
            if (newCertificate.RetireParent &&
                (string.IsNullOrWhiteSpace(newCertificate.DeviceId) ||
                 newCertificate.DeviceId.Any(c => c < ' ' || c > '~')))
                throw new BadRequestException("Invalid retirement device identifier");
            var verify = CertHelper.VerifyData(newCertificate.SigningPayload(), newCertificate.ParentCertHashSignature,
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

        void ValidateCertificate(CertificateUpdateDto newPublicCertDto, CertificateDb oldPublicCertDb);

        void ValidateCertificateChain(CertificateUpdateDto newCertificate, CertificateDb oldCertificate);
    }
}
