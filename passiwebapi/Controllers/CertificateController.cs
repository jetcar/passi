using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Models;
using passi_webapi.Dto;
using Repos;
using Services;
using WebApiDto;
using WebApiDto.Certificate;

namespace passi_webapi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CertificateController : ControllerBase
    {

        ICertificateRepository _certificateRepository;
        ICertificatesService _certificatesService;
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
            using (var transaction = _certificateRepository.BeginTransaction())
            {
                var certificateDb = _certificateRepository.GetUserCertificate(username, thumbprint);
                return new CertificateDto()
                {
                    PublicCert = certificateDb?.PublicCert,
                };
            }

        }

        [HttpPost, Route("UpdatePublicCert")]
        public CertificateUpdateDto UpdatePublicCert([FromBody] CertificateUpdateDto newCertificate)
        {
            using (var transaction = _certificateRepository.BeginTransaction())
            {
                var oldCertificate = _certificateRepository.GetCertificate(newCertificate.ParentCertThumbprint);

                _certValidator.ValidateCertificateChain(newCertificate, oldCertificate);
                _certValidator.ValidateCertificate(newCertificate, oldCertificate);

                var certificateDb = _certificatesService.UpdateCertificate(newCertificate);
                transaction.Commit();
                return new CertificateUpdateDto()
                {
                    PublicCert = certificateDb?.PublicCert
                };

            }

        }
    }


}
