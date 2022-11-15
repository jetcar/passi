using System;

namespace WebApiDto.Auth
{
    public class AuthorizeDto
    {
        public string SignedHash { get; set; }
        public string PublicCertThumbprint { get; set; }
        public Guid SessionId { get; set; }
    }
}