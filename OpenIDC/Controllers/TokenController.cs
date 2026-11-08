using System;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using OpenIDC.Models;
using OpenIDC.Services;
using OpenIDC.Helpers;

namespace OpenIDC.Controllers
{
    [IgnoreAntiforgeryToken]
    public class TokenController : Controller
    {
        private readonly IClientStore _clientStore;
        private readonly IAuthorizationCodeStore _authCodeStore;
        private readonly ITokenService _tokenService;
        private readonly IRefreshTokenStore _refreshTokenStore;
        private readonly ILogger<TokenController> _logger;
        private readonly ResourcePolicy _resources;

        public TokenController(
            IClientStore clientStore,
            IAuthorizationCodeStore authCodeStore,
            ITokenService tokenService,
            IRefreshTokenStore refreshTokenStore,
            ILogger<TokenController> logger, ResourcePolicy resources = null)
        {
            _clientStore = clientStore;
            _authCodeStore = authCodeStore;
            _tokenService = tokenService;
            _refreshTokenStore = refreshTokenStore;
            _logger = logger;
            _resources = resources ?? new ResourcePolicy();
        }

        [HttpPost("connect/token")]
        [Consumes("application/x-www-form-urlencoded")]
        public async Task<IActionResult> Token([FromForm] TokenRequest request)
        {
            if (HttpContext != null)
            {
                Response.Headers.CacheControl = "no-store";
                Response.Headers.Pragma = "no-cache";
                if (Request.HasFormContentType)
                {
                    var form = await Request.ReadFormAsync();
                    if (form.Any(x => x.Value.Count != 1)) return BadRequest(new { error = "invalid_request" });
                    if (form.ContainsKey("resource") && string.IsNullOrWhiteSpace(form["resource"]))
                        return BadRequest(new { error = "invalid_target" });
                    if (request != null && form.ContainsKey("scope")) request.Scope = form["scope"].ToString();
                }
            }
            if (request == null || string.IsNullOrWhiteSpace(request.ClientId) &&
                string.IsNullOrEmpty(HttpContext?.Request.Headers["Authorization"]))
                return BadRequest(new { error = "invalid_request" });
            if (!_resources.IsAllowed(request.Resource))
                return BadRequest(new { error = "invalid_target" });
            // Extract client credentials from Basic Auth header if not in form
            if (string.IsNullOrEmpty(request.ClientId) || string.IsNullOrEmpty(request.ClientSecret))
            {
                var authHeader = Request.Headers["Authorization"].ToString();
                if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        var encodedCredentials = authHeader.Substring("Basic ".Length).Trim();
                        var decodedCredentials = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(encodedCredentials));
                        var parts = decodedCredentials.Split(':', 2);

                        if (parts.Length == 2)
                        {
                            if (string.IsNullOrEmpty(request.ClientId))
                                request.ClientId = parts[0];
                            if (string.IsNullOrEmpty(request.ClientSecret))
                                request.ClientSecret = parts[1];

                            _logger.LogDebug("Client credentials extracted from Basic Auth header");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to parse Basic Auth header");
                    }
                }
            }

            _logger.LogInformation("Token request received");

            var normalizedGrantType = request.GrantType?.Trim();
            if (string.IsNullOrEmpty(normalizedGrantType) ||
                (normalizedGrantType != "authorization_code" && normalizedGrantType != "refresh_token"))
            {
                _logger.LogWarning("Unsupported grant type: {GrantType}", LogValue.Safe(request.GrantType));
                return BadRequest(new { error = "unsupported_grant_type" });
            }

            if (normalizedGrantType == "authorization_code")
            {
                return await HandleAuthorizationCode(request);
            }
            else if (normalizedGrantType == "refresh_token")
            {
                return await HandleRefreshToken(request);
            }

            _logger.LogWarning("Unsupported grant type: {GrantType}", LogValue.Safe(request.GrantType));
            return BadRequest(new { error = "unsupported_grant_type" });
        }

        private async Task<IActionResult> HandleAuthorizationCode(TokenRequest request)
        {
            _logger.LogInformation("Handling authorization code exchange for client: {ClientId}", LogValue.Safe(request.ClientId));

            // Validate client exists
            var client = await _clientStore.GetClientAsync(request.ClientId);
            if (client == null)
            {
                _logger.LogWarning("Client not found: {ClientId}", LogValue.Safe(request.ClientId));
                return Unauthorized(new { error = "invalid_client" });
            }

            _logger.LogDebug("Client validated successfully: {ClientId}", LogValue.Safe(request.ClientId));

            // Consume the authorization code first (atomically, so a replayed/concurrent request for the
            // same code can never also get a non-null result) to check if PKCE was used
            var authCode = await _authCodeStore.ConsumeAuthorizationCodeAsync(request.Code);
            if (authCode == null || authCode.ExpiresAt < DateTime.UtcNow)
            {
                _logger.LogWarning("Invalid or expired authorization code");
                return BadRequest(new { error = "invalid_grant", error_description = "Invalid or expired authorization code" });
            }

            // Determine authentication method
            bool hasPkce = !string.IsNullOrEmpty(authCode.CodeChallenge);
            bool hasClientSecret = !string.IsNullOrEmpty(request.ClientSecret);
            if (!_resources.Matches(authCode.Resource, request.Resource))
                return BadRequest(new { error = "invalid_target" });
            if ((client.RequiresPkce || client.IsPublicClient || client.IsDynamic || authCode.Resource != null) && !hasPkce)
                return BadRequest(new { error = "invalid_grant", error_description = "PKCE S256 is required" });
            if (client.IsDynamic && (hasClientSecret || Request.Headers.ContainsKey("Authorization")))
                return Unauthorized(new { error = "invalid_client" });

            // Confidential clients (user-registered websites) must always authenticate with their secret
            if (client.RequireClientSecret && !hasClientSecret)
            {
                _logger.LogWarning("Client secret required but not provided for client: {ClientId}", LogValue.Safe(request.ClientId));
                return Unauthorized(new { error = "invalid_client" });
            }

            // Require at least one authentication method
            if (!hasPkce && !hasClientSecret)
            {
                _logger.LogWarning("No authentication method provided (neither PKCE nor client secret)");
                return BadRequest(new { error = "invalid_request", error_description = "Authentication required: provide either PKCE or client secret" });
            }

            // If client secret is provided, validate it
            if (hasClientSecret)
            {
                if (!await _clientStore.ValidateClientAsync(request.ClientId, request.ClientSecret))
                {
                    _logger.LogWarning("Client secret validation failed for client: {ClientId}", LogValue.Safe(request.ClientId));
                    return Unauthorized(new { error = "invalid_client" });
                }
                _logger.LogDebug("Client authenticated with secret successfully");
            }

            // Validate client ID matches
            if (authCode.ClientId != request.ClientId)
            {
                _logger.LogWarning("Client ID mismatch. Expected: {Expected}, Got: {Got}", LogValue.Safe(authCode.ClientId), LogValue.Safe(request.ClientId));
                return BadRequest(new { error = "invalid_grant", error_description = "Client ID mismatch" });
            }

            // Validate redirect URI
            if (authCode.RedirectUri != request.RedirectUri)
            {
                _logger.LogWarning("Redirect URI mismatch");
                return BadRequest(new { error = "invalid_grant", error_description = "Redirect URI mismatch" });
            }

            _logger.LogDebug("Authorization code validations passed. Subject: {Subject}", LogValue.Safe(authCode.Subject));

            // Validate PKCE if it was used during authorization
            if (hasPkce)
            {
                if (string.IsNullOrEmpty(request.CodeVerifier))
                {
                    _logger.LogWarning("Code verifier required but not provided (PKCE was used during authorization)");
                    return BadRequest(new { error = "invalid_request", error_description = "Code verifier required for PKCE" });
                }

                if (!PkceHelper.ValidateCodeChallenge(request.CodeVerifier, authCode.CodeChallenge, authCode.CodeChallengeMethod))
                {
                    _logger.LogWarning("PKCE validation failed");
                    return BadRequest(new { error = "invalid_grant", error_description = "Invalid code verifier" });
                }

                _logger.LogDebug("PKCE validation successful");
            }

            // Generate tokens
            _logger.LogInformation("Generating tokens for subject: {Subject}, client: {ClientId}", LogValue.Safe(authCode.Subject), LogValue.Safe(authCode.ClientId));
            var accessToken = _tokenService.GenerateAccessToken(authCode.Subject, authCode.ClientId, authCode.Scopes, authCode.Claims, authCode.Resource);
            var idToken = authCode.Scopes.Contains("openid")
                ? _tokenService.GenerateIdToken(authCode.Subject, authCode.ClientId, authCode.Scopes, authCode.Claims, authCode.Nonce) : null;

            // Generate refresh token
            var refreshToken = await _refreshTokenStore.CreateRefreshTokenAsync(new RefreshToken
            {
                ClientId = authCode.ClientId,
                Resource = authCode.Resource,
                ClientExpiresAt = client.ExpiresAt,
                Subject = authCode.Subject,
                Scopes = authCode.Scopes,
                Claims = authCode.Claims
            });

            _logger.LogInformation("Token exchange successful for subject: {Subject}", LogValue.Safe(authCode.Subject));

            return TokenResponse(accessToken, idToken, refreshToken, authCode.Scopes);
        }

        private async Task<IActionResult> HandleRefreshToken(TokenRequest request)
        {
            // Validate client (public clients have no secret; the refresh token itself is the credential)
            var refreshClient = await _clientStore.GetClientAsync(request.ClientId);
            if (refreshClient == null || (refreshClient.IsDynamic &&
                (!string.IsNullOrEmpty(request.ClientSecret) || Request.Headers.ContainsKey("Authorization"))))
                return Unauthorized(new { error = "invalid_client" });
            var isPublicWithoutSecret = refreshClient != null && refreshClient.IsPublicClient && string.IsNullOrEmpty(request.ClientSecret);
            if (!isPublicWithoutSecret && !await _clientStore.ValidateClientAsync(request.ClientId, request.ClientSecret))
            {
                return Unauthorized(new { error = "invalid_client" });
            }

            // Atomically consume the refresh token so two concurrent refresh requests presenting the
            // same token can never both succeed (see
            // RefreshTokenStoreConcurrencyTests.ConcurrentTokenRefreshesCannotBothConsumeTheSameRefreshToken).
            var refreshToken = await _refreshTokenStore.ConsumeRefreshTokenAsync(request.RefreshToken);
            if (refreshToken == null)
            {
                // Usually a token that was already rotated, or one lost from Redis (e.g. a restart before it was persisted).
                _logger.LogWarning("Refresh token not found for client: {ClientId}", LogValue.Safe(request.ClientId));
                return BadRequest(new { error = "invalid_grant", error_description = "Invalid or expired refresh token" });
            }
            if (refreshToken.ExpiresAt < DateTime.UtcNow)
            {
                _logger.LogWarning("Refresh token expired at {ExpiresAt:o} for client: {ClientId}, subject: {Subject}",
                    refreshToken.ExpiresAt, LogValue.Safe(request.ClientId), LogValue.Safe(refreshToken.Subject));
                return BadRequest(new { error = "invalid_grant", error_description = "Invalid or expired refresh token" });
            }

            // Validate client ID matches
            if (refreshToken.ClientId != request.ClientId)
            {
                _logger.LogWarning("Refresh token client ID mismatch. Expected: {Expected}, Got: {Got}", LogValue.Safe(refreshToken.ClientId), LogValue.Safe(request.ClientId));
                return BadRequest(new { error = "invalid_grant", error_description = "Client ID mismatch" });
            }

            if (!_resources.Matches(refreshToken.Resource, request.Resource))
                return BadRequest(new { error = "invalid_target" });
            if (request.Scope != null && !request.Scope.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .ToHashSet(StringComparer.Ordinal).SetEquals(refreshToken.Scopes))
                return BadRequest(new { error = "invalid_scope" });

            // Generate new tokens
            var accessToken = _tokenService.GenerateAccessToken(refreshToken.Subject, refreshToken.ClientId, refreshToken.Scopes, refreshToken.Claims, refreshToken.Resource);
            var idToken = refreshToken.Scopes.Contains("openid")
                ? _tokenService.GenerateIdToken(refreshToken.Subject, refreshToken.ClientId, refreshToken.Scopes, refreshToken.Claims, null) : null;

            var newRefreshToken = await _refreshTokenStore.CreateRefreshTokenAsync(new RefreshToken
            {
                ClientId = refreshToken.ClientId,
                FamilyId = refreshToken.FamilyId,
                Resource = refreshToken.Resource,
                ClientExpiresAt = refreshToken.ClientExpiresAt ?? refreshClient.ExpiresAt,
                Subject = refreshToken.Subject,
                Scopes = refreshToken.Scopes,
                Claims = refreshToken.Claims
            });

            return TokenResponse(accessToken, idToken, newRefreshToken, refreshToken.Scopes);
        }
        private IActionResult TokenResponse(string access, string id, string refresh, List<string> scopes)
        {
            var response = new Dictionary<string, object>
            {
                ["access_token"] = access, ["token_type"] = "Bearer", ["expires_in"] = 3600,
                ["refresh_token"] = refresh, ["scope"] = string.Join(" ", scopes),
            };
            if (id != null) response["id_token"] = id;
            return Ok(response);
        }
    }

    public class TokenRequest
    {
        [FromForm(Name = "resource")]
        public string Resource { get; set; }
        [FromForm(Name = "scope")]
        public string Scope { get; set; }

        [FromForm(Name = "grant_type")]
        public string GrantType { get; set; }

        [FromForm(Name = "code")]
        public string Code { get; set; }

        [FromForm(Name = "redirect_uri")]
        public string RedirectUri { get; set; }

        [FromForm(Name = "client_id")]
        public string ClientId { get; set; }

        [FromForm(Name = "client_secret")]
        public string ClientSecret { get; set; }

        [FromForm(Name = "code_verifier")]
        public string CodeVerifier { get; set; }

        [FromForm(Name = "refresh_token")]
        public string RefreshToken { get; set; }
    }
}
