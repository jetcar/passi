using System;
using System.Collections.Generic;

namespace OpenIDC.Models
{
    public class AuthorizationCode
    {
        public string Code { get; set; }
        public string ClientId { get; set; }
        public string Subject { get; set; }
        public string RedirectUri { get; set; }
        public List<string> Scopes { get; set; } = new List<string>();
        public DateTime ExpiresAt { get; set; }
        public string CodeChallenge { get; set; }
        public string CodeChallengeMethod { get; set; }
        public string Nonce { get; set; }
        public Dictionary<string, string> Claims { get; set; } = new Dictionary<string, string>();
    }
}
