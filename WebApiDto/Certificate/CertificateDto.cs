using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace WebApiDto.Certificate
{
    public class CertificateDto
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
