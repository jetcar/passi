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

        /// <summary>SHA-256 hex of the secret for user-registered clients (plaintext is never stored).</summary>
        public string ClientSecretHash { get; set; }

        /// <summary>Confidential client: the token endpoint must receive a valid client secret.</summary>
        public bool RequireClientSecret { get; set; }

        /// <summary>Public client (no secret): relies on PKCE; refresh does not require a secret.</summary>
        public bool IsPublicClient { get; set; }
    }
}
