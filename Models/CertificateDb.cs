using NodaTime;
using System;
using System.ComponentModel.DataAnnotations;
using System.Reflection.Metadata;

namespace Models
{
    public class CertificateDb : BaseModel
    {
        public string Thumbprint { get; set; }
        public long UserId { get; set; }
        public string PublicCert { get; set; }
        public string PrivateCert { get; set; }
        public string ParentCertSignature { get; set; }
        public string ParentCertId { get; set; }
        public virtual CertificateDb ParentCert { get; set; }
        public virtual UserDb User { get; set; }
        public virtual CertificateDb InverseParentCert { get; set; }
    }
}