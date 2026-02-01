using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using OpenIDC.Models;
using OpenIDC.Services;
using OpenIDC.Helpers;

namespace OpenIDC.Controllers
{
    public class TokenController : Controller
    {
        private readonly IClientStore _clientStore;
        private readonly IAuthorizationCodeStore _authCodeStore;
        private readonly ITokenService _tokenService;
        private readonly IRefreshTokenStore _refreshTokenStore;
        private readonly ILogger<TokenController> _logger;

        public TokenController(
            IClientStore clientStore,
            IAuthorizationCodeStore authCodeStore,
            ITokenService tokenService,
            IRefreshTokenStore refreshTokenStore,
            ILogger<TokenController> logger)
        {
            _clientStore = clientStore;
            _authCodeStore = authCodeStore;
            _tokenService = tokenService;
            _refreshTokenStore = refreshTokenStore;
            _logger = logger;
        }

        [HttpPost("connect/token")]
        [Consumes("application/x-www-form-urlencoded")]
        public async Task<IActionResult> Token([FromForm] TokenRequest request)
        {
            _logger.LogInformation("Token request received - GrantType: {GrantType}, ClientId: {ClientId}, Code: {Code}",
                request.GrantType, request.ClientId, request.Code?.Substring(0, Math.Min(8, request.Code?.Length ?? 0)) + "...");

            if (request.GrantType == "authorization_code")
            {
                return await HandleAuthorizationCode(request);
            }
            else if (request.GrantType == "refresh_token")
            {
                return await HandleRefreshToken(request);
            }

            _logger.LogWarning("Unsupported grant type: {GrantType}", request.GrantType);
            return BadRequest(new { error = "unsupported_grant_type" });
        }

        private async Task<IActionResult> HandleAuthorizationCode(TokenRequest request)
        {
            _logger.LogInformation("Handling authorization code exchange for client: {ClientId}", request.ClientId);

            // Validate client exists
            var client = await _clientStore.GetClientAsync(request.ClientId);
            if (client == null)
            {
                _logger.LogWarning("Client not found: {ClientId}", request.ClientId);
                return Unauthorized(new { error = "invalid_client" });
            }

            _logger.LogDebug("Client validated successfully: {ClientId}", request.ClientId);

            // Get authorization code first to check if PKCE was used
            var authCode = await _authCodeStore.GetAuthorizationCodeAsync(request.Code);
            if (authCode == null || authCode.ExpiresAt < DateTime.UtcNow)
            {
                _logger.LogWarning("Invalid or expired authorization code. Code: {Code}, Expired: {Expired}",
                    request.Code?.Substring(0, Math.Min(8, request.Code?.Length ?? 0)) + "...",
                    authCode?.ExpiresAt < DateTime.UtcNow);
                return BadRequest(new { error = "invalid_grant", error_description = "Invalid or expired authorization code" });
            }

            // Determine authentication method
            bool hasPkce = !string.IsNullOrEmpty(authCode.CodeChallenge);
            bool hasClientSecret = !string.IsNullOrEmpty(request.ClientSecret);
            bool clientHasSecretConfigured = !string.IsNullOrEmpty(client.ClientSecret);

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
                    _logger.LogWarning("Client secret validation failed for client: {ClientId}", request.ClientId);
                    return Unauthorized(new { error = "invalid_client" });
                }
                _logger.LogDebug("Client authenticated with secret successfully");
            }

            // Validate client ID matches
            if (authCode.ClientId != request.ClientId)
            {
                _logger.LogWarning("Client ID mismatch. Expected: {Expected}, Got: {Got}", authCode.ClientId, request.ClientId);
                return BadRequest(new { error = "invalid_grant", error_description = "Client ID mismatch" });
            }

            // Validate redirect URI
            if (authCode.RedirectUri != request.RedirectUri)
            {
                _logger.LogWarning("Redirect URI mismatch. Expected: {Expected}, Got: {Got}", authCode.RedirectUri, request.RedirectUri);
                return BadRequest(new { error = "invalid_grant", error_description = "Redirect URI mismatch" });
            }

            _logger.LogDebug("Authorization code validations passed. Subject: {Subject}", authCode.Subject);

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

            // Revoke the authorization code (one-time use)
            await _authCodeStore.RevokeAuthorizationCodeAsync(request.Code);
            _logger.LogDebug("Authorization code revoked");

            // Generate tokens
            _logger.LogInformation("Generating tokens for subject: {Subject}, client: {ClientId}", authCode.Subject, authCode.ClientId);
            var accessToken = _tokenService.GenerateAccessToken(authCode.Subject, authCode.ClientId, authCode.Scopes, authCode.Claims);
            var idToken = _tokenService.GenerateIdToken(authCode.Subject, authCode.ClientId, authCode.Scopes, authCode.Claims, authCode.Nonce);

            // Generate refresh token
            var refreshToken = await _refreshTokenStore.CreateRefreshTokenAsync(new RefreshToken
            {
                ClientId = authCode.ClientId,
                Subject = authCode.Subject,
                Scopes = authCode.Scopes,
                Claims = authCode.Claims
            });

            _logger.LogInformation("Token exchange successful for subject: {Subject}", authCode.Subject);

            return Ok(new
            {
                access_token = accessToken,
                token_type = "Bearer",
                expires_in = 3600,
                id_token = idToken,
                refresh_token = refreshToken
            });
        }

        private async Task<IActionResult> HandleRefreshToken(TokenRequest request)
        {
            // Validate client
            if (!await _clientStore.ValidateClientAsync(request.ClientId, request.ClientSecret))
            {
                return Unauthorized(new { error = "invalid_client" });
            }

            // Get refresh token
            var refreshToken = await _refreshTokenStore.GetRefreshTokenAsync(request.RefreshToken);
            if (refreshToken == null || refreshToken.ExpiresAt < DateTime.UtcNow)
            {
                return BadRequest(new { error = "invalid_grant", error_description = "Invalid or expired refresh token" });
            }

            // Validate client ID matches
            if (refreshToken.ClientId != request.ClientId)
            {
                return BadRequest(new { error = "invalid_grant", error_description = "Client ID mismatch" });
            }

            // Generate new tokens
            var accessToken = _tokenService.GenerateAccessToken(refreshToken.Subject, refreshToken.ClientId, refreshToken.Scopes, refreshToken.Claims);
            var idToken = _tokenService.GenerateIdToken(refreshToken.Subject, refreshToken.ClientId, refreshToken.Scopes, refreshToken.Claims, null);

            // Optionally rotate refresh token
            await _refreshTokenStore.RevokeRefreshTokenAsync(request.RefreshToken);
            var newRefreshToken = await _refreshTokenStore.CreateRefreshTokenAsync(new RefreshToken
            {
                ClientId = refreshToken.ClientId,
                Subject = refreshToken.Subject,
                Scopes = refreshToken.Scopes,
                Claims = refreshToken.Claims
            });

            return Ok(new
            {
                access_token = accessToken,
                token_type = "Bearer",
                expires_in = 3600,
                id_token = idToken,
                refresh_token = newRefreshToken
            });
        }
    }

    public class TokenRequest
    {
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
