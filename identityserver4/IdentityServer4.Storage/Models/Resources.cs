﻿using System.Collections.Generic;
using System.Linq;
using PostSharp.Extensibility;

namespace IdentityServer4.Storage.Models
{
    /// <summary>
    /// Models a collection of identity and API resources.
    /// </summary>
    [Profile(AttributeTargetElements = MulticastTargets.Method)]
    public class Resources
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Resources"/> class.
        /// </summary>
        public Resources()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Resources"/> class.
        /// </summary>
        /// <param name="other">The other.</param>
        public Resources(Resources other)
            : this(other.IdentityResources, other.ApiScopes)
        {
            OfflineAccess = other.OfflineAccess;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Resources"/> class.
        /// </summary>
        /// <param name="identityResources">The identity resources.</param>
        /// <param name="apiResources">The API resources.</param>
        /// <param name="apiScopes">The API scopes.</param>
        public Resources(IEnumerable<IdentityResource> identityResources, IEnumerable<ApiScope> apiScopes)
        {
            if (identityResources?.Any() == true)
            {
                IdentityResources = new HashSet<IdentityResource>(identityResources.ToArray());
            }

            ApiResources = new HashSet<ApiResource>();

            if (apiScopes?.Any() == true)
            {
                ApiScopes = new HashSet<ApiScope>(apiScopes.ToArray());
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether [offline access].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [offline access]; otherwise, <c>false</c>.
        /// </value>
        public bool OfflineAccess { get; set; }

        /// <summary>
        /// Gets or sets the identity resources.
        /// </summary>
        public ICollection<IdentityResource> IdentityResources { get; set; } = new HashSet<IdentityResource>();

        /// <summary>
        /// Gets or sets the API resources.
        /// </summary>
        public ICollection<ApiResource> ApiResources { get; set; } = new HashSet<ApiResource>();

        /// <summary>
        /// Gets or sets the API scopes.
        /// </summary>
        public ICollection<ApiScope> ApiScopes { get; set; } = new HashSet<ApiScope>();
    }
}