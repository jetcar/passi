using System;

namespace WebApiDto.Auth.Dto
{
    public class SessionMinDto
    {
        public string SignedHash { get; set; }
        public string PublicCert { get; set; }
        public DateTime ExpirationTime { get; set; }
    }
}