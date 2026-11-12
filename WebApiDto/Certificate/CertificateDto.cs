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

        // Opt-in device key migration. This changes the signed payload; legacy enrollment
        // continues to sign PublicCert alone and never retires an existing device's key.
        public bool RetireParent { get; set; }

        public string SigningPayload() => RetireParent
            ? $"Passi:RetireParent:v2\n{ParentCertThumbprint}\n{DeviceId}\n{PublicCert}"
            : PublicCert;
    }
}
