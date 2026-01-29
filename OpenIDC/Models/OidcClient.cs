using System;
using System.Collections.Generic;

namespace OpenIDC.Models
{
    public class OidcClient
    {
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string DisplayName { get; set; }
        public List<string> RedirectUris { get; set; } = new List<string>();
        public List<string> AllowedScopes { get; set; } = new List<string>();
        public List<string> GrantTypes { get; set; } = new List<string>();
        public bool RequiresPkce { get; set; }
        public string ConsentType { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
