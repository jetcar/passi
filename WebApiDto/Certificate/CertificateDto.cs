using System.ComponentModel.DataAnnotations;

namespace WebApiDto.Certificate
{
    public class CertificateUpdateDto
    {
        [Required]
        public string PublicCert { get; set; }

        [Required]
        public string ParentCertThumbprint { get; set; }

        [Required]
        public string ParentCertHashSignature { get; set; }

        [Required]
        public string DeviceId { get; set; }
    }
}