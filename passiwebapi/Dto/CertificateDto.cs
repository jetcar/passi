﻿namespace passi_webapi.Dto
{
    public class CertificateDto
    {
        public string Thumbprint { get; set; }
        public long UserId { get; set; }
        public string PublicCert { get; set; }
        public string PrivateCert { get; set; }
        public string ParentCertSignature { get; set; }
        public string ParentCertId { get; set; }
    }
}