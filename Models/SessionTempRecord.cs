using NodaTime;
using System;
using System.ComponentModel.DataAnnotations;

namespace Models
{
    public class SimpleSessionDb : BaseModel
    {
        public Guid Guid { get; set; }
        public long UserId { get; set; }
        public virtual UserDb User { get; set; }
        public SessionStatus? Status { get; set; }
        public Instant ExpirationTime { get; set; }
        public string SignedHashNew { get; set; }

    }
    public class SessionTempRecord
    {
        public Guid Guid { get; set; }
        public long UserId { get; set; }
        public string ClientId { get; set; }
        public string RandomString { get; set; }
        public SessionStatus? Status { get; set; }
        public string PublicCertThumbprint { get; set; }
        public string CheckColor { get; set; }
        public string ReturnUrl { get; set; }
        public DateTime ExpirationTime { get; set; }
        public string ErrorMessage { get; set; }
        public string Email { get; set; }
        public Guid UserGuid { get; set; }
    }
}