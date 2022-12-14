using Models;
using System;
using NodaTime;

namespace passi_webapi.Dto
{
    public class SessionDto
    {
        public Guid Guid { get; set; }
        public long UserId { get; set; }
        public string ClientId { get; set; }
        public string RandomString { get; set; }
        public SessionStatus? Status { get; set; }
        public string SignedHash { get; set; }
        public string PublicCertThumbprint { get; set; }
        public string CheckColor { get; set; }
        public string ReturnUrl { get; set; }
        public Instant ExpirationTime { get; set; }
        public string ErrorMessage { get; set; }
    }
}