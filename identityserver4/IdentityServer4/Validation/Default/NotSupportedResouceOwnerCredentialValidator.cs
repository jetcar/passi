// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using IdentityServer4.Models;
using IdentityServer4.Validation.Contexts;
using IdentityServer4.Validation.Models;
using Microsoft.Extensions.Logging;

using System.Threading.Tasks;

namespace IdentityServer4.Validation.Default
{
    /// <summary>
    /// Default resource owner password validator (no implementation == not supported)
    /// </summary>
    /// <seealso cref="IdentityServer4.Validation.IResourceOwnerPasswordValidator" />
    [GoogleTracer.Profile]
    public class NotSupportedResourceOwnerPasswordValidator : IResourceOwnerPasswordValidator
    {
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="NotSupportedResourceOwnerPasswordValidator"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        public NotSupportedResourceOwnerPasswordValidator(ILogger<NotSupportedResourceOwnerPasswordValidator> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Validates the resource owner password credential
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns></returns>
        public Task ValidateAsync(ResourceOwnerPasswordValidationContext context)
        {
            context.Result = new GrantValidationResult(TokenRequestErrors.UnsupportedGrantType);

            _logger.LogInformation("Resource owner password credential type not supported. Configure an IResourceOwnerPasswordValidator.");
            return Task.CompletedTask;
        }
    }
}