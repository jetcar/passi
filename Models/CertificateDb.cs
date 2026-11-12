namespace Models
{
    public class CertificateDb : BaseModel
    {
        public string Thumbprint { get; set; }
        public long UserId { get; set; }
        public string PublicCert { get; set; }
        public bool IsRetired { get; set; }
        // Legacy nullable schema column, retained for existing databases. Enrollment and
        // rotation store public certificates only; never populate this field with key material.
        public string PrivateCert { get; set; }
        public string ParentCertSignature { get; set; }
        public string ParentCertId { get; set; }
        public virtual CertificateDb ParentCert { get; set; }
        public virtual UserDb User { get; set; }
        public virtual System.Collections.Generic.ICollection<CertificateDb> ChildCertificates { get; set; }
            = new System.Collections.Generic.List<CertificateDb>();
    }
}
