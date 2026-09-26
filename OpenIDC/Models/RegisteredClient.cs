using System;
using System.Collections.Generic;

namespace OpenIDC.Models
{
    public static class RegisteredClientTypes
    {
        /// <summary>Server-side website: confidential client authenticated with a client secret.</summary>
        public const string Web = "web";

        /// <summary>Mobile/desktop/SPA app: public client, no secret, PKCE required.</summary>
        public const string App = "app";
    }

    /// <summary>An OAuth/OIDC client registered by a Passi user (persisted in Postgres).</summary>
    public class RegisteredClient
    {
        public Guid Id { get; set; }
        public string ClientId { get; set; }

        /// <summary>SHA-256 hex of the client secret; null for public (app) clients.</summary>
        public string ClientSecretHash { get; set; }

        public string DisplayName { get; set; }
        public string ClientType { get; set; }
        public List<string> RedirectUris { get; set; } = new List<string>();

        /// <summary>Subject (sub claim) of the user who registered and manages this client.</summary>
        public string OwnerSubject { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
