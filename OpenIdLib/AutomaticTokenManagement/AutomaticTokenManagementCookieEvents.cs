﻿using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using PostSharp.Extensibility;
using Repos;

namespace OpenIdLib.AutomaticTokenManagement
{
    [Profile(AttributeTargetElements = MulticastTargets.Method)]
    public class AutomaticTokenManagementCookieEvents : CookieAuthenticationEvents
    {
        private readonly TokenEndpointService _service;
        private readonly AutomaticTokenManagementOptions _options;
        private readonly ILogger _logger;

        private static readonly ConcurrentDictionary<string, bool> _pendingRefreshTokenRequests =
            new ConcurrentDictionary<string, bool>();

        public AutomaticTokenManagementCookieEvents(
            TokenEndpointService service,
            IOptions<AutomaticTokenManagementOptions> options,
            ILogger<AutomaticTokenManagementCookieEvents> logger
            )
        {
            _service = service;
            _options = options.Value;
            _logger = logger;
        }

        public override async Task ValidatePrincipal(CookieValidatePrincipalContext context)
        {
            var tokens = context.Properties.GetTokens();
            if (tokens == null || !tokens.Any())
            {
                _logger.LogDebug("No tokens found in cookie properties. SaveTokens must be enabled for automatic token refresh.");
                return;
            }

            var refreshToken = tokens.SingleOrDefault(t => t.Name == OpenIdConnectParameterNames.RefreshToken);
            if (refreshToken == null)
            {
                _logger.LogWarning("No refresh token found in cookie properties. A refresh token must be requested and SaveTokens must be enabled.");
                return;
            }

            var expiresAt = tokens.SingleOrDefault(t => t.Name == "expires_at");
            if (expiresAt == null)
            {
                _logger.LogWarning("No expires_at value found in cookie properties.");
                return;
            }

            var dtExpires = DateTimeOffset.Parse(expiresAt.Value, CultureInfo.InvariantCulture);
            var dtRefresh = dtExpires.Subtract(_options.RefreshBeforeExpiration);

            if (dtRefresh < DateTime.UtcNow)
            {
                var shouldRefresh = _pendingRefreshTokenRequests.TryAdd(refreshToken.Value, true);
                if (shouldRefresh)
                {
                    try
                    {
                        var response = await _service.RefreshTokenAsync(refreshToken.Value);

                        if (response.IsError)
                        {
                            _logger.LogWarning("Error refreshing token: {error}", response.Error);
                            // Sign-out user in case refresh token is no longer valid
                            await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                            return;
                        }

                        context.Properties.UpdateTokenValue("access_token", response.AccessToken);
                        context.Properties.UpdateTokenValue("refresh_token", response.RefreshToken);

                        var newExpiresAt = DateTime.UtcNow + TimeSpan.FromSeconds(response.ExpiresIn);
                        context.Properties.UpdateTokenValue("expires_at", newExpiresAt.ToString("o", CultureInfo.InvariantCulture));
                        _logger.LogInformation("successfully got new refresh token");
                        await context.HttpContext.SignInAsync(context.Principal, context.Properties);
                    }
                    finally
                    {
                        _pendingRefreshTokenRequests.TryRemove(refreshToken.Value, out _);
                    }
                }
            }
        }

        public override async Task SigningOut(CookieSigningOutContext context)
        {
            if (_options.RevokeRefreshTokenOnSignout == false) return;

            var result = await context.HttpContext.AuthenticateAsync();

            if (!result.Succeeded)
            {
                _logger.LogDebug("Can't find cookie for default scheme. Might have been deleted already.");
                return;
            }

            var tokens = result.Properties.GetTokens();
            if (tokens == null || !tokens.Any())
            {
                _logger.LogDebug("No tokens found in cookie properties. SaveTokens must be enabled for automatic token revocation.");
                return;
            }

            var refreshToken = tokens.SingleOrDefault(t => t.Name == OpenIdConnectParameterNames.RefreshToken);
            if (refreshToken == null)
            {
                _logger.LogWarning("No refresh token found in cookie properties. A refresh token must be requested and SaveTokens must be enabled.");
                return;
            }

            var response = await _service.RevokeTokenAsync(refreshToken.Value);

            if (response.IsError)
            {
                _logger.LogWarning("Error revoking token: {error}", response.Error);
                return;
            }
        }
    }
}