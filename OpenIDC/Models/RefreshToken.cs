using System;
using System.Collections.Generic;

namespace OpenIDC.Models
{
    public class RefreshToken
    {
        public string Token { get; set; }
        public string ClientId { get; set; }
        public string Subject { get; set; }
        public List<string> Scopes { get; set; } = new List<string>();
        public DateTime ExpiresAt { get; set; }
        public Dictionary<string, string> Claims { get; set; } = new Dictionary<string, string>();
    }
}
