using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using passi_webapi.Dto;
using PostSharp.Extensibility;
using Repos;
using Services;
using WebApiDto.Certificate;

namespace passi_webapi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Profile(AttributeTargetElements = MulticastTargets.Method)]
    public class CertificateController : ControllerBase
    {
        private ICertificateRepository _certificateRepository;
        private ICertificatesService _certificatesService;
        private ICertValidator _certValidator;

        public CertificateController(ICertificateRepository certificateRepository, ICertificatesService certificatesService, ICertValidator certValidator)
        {
            _certificateRepository = certificateRepository;
            _certificatesService = certificatesService;
            _certValidator = certValidator;
        }

        [HttpGet, Route("Public")]
        public CertificateDto PublicCert([FromQuery] string thumbprint, string username)
        {
            var certificateDb = _certificateRepository.GetUserCertificate(username, thumbprint);
            return new CertificateDto()
            {
                PublicCert = certificateDb?.PublicCert,
            };
        }

        [HttpPost, Route("UpdatePublicCert")]
        public CertificateUpdateDto UpdatePublicCert([FromBody] CertificateUpdateDto newCertificate)
        {
            string cert = "";
            var strategy = _certificateRepository.GetExecutionStrategy();
            strategy.Execute(() =>
            {
                using (var transaction = _certificateRepository.BeginTransaction())
                {
                    var oldCertificate = _certificateRepository.GetCertificate(newCertificate.ParentCertThumbprint);

                    _certValidator.ValidateCertificateChain(newCertificate, oldCertificate);
                    _certValidator.ValidateCertificate(newCertificate, oldCertificate);

                    var certificateDb = _certificatesService.UpdateCertificate(newCertificate);
                    transaction.Commit();
                    cert = certificateDb?.PublicCert;
                }
            });
            return new CertificateUpdateDto()
            {
                PublicCert = cert
            };
        }
    }
}