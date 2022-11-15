using NodaTime;
using System;
using System.ComponentModel.DataAnnotations;

namespace Models
{
    public class SessionDb : BaseModel
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

        public virtual UserDb User { get; set; }
        public string ErrorMessage { get; set; }
    }

    public enum SessionStatus
    {
        Canceled = 1,
        Confirmed,
        Error
    }
}