using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenIDC.Services;
using ConfigurationManager;

namespace OpenIDC.Controllers
{
    public class AuthorizationController : Controller
    {
        private readonly IClientStore _clientStore;
        private readonly IAuthorizationCodeStore _authCodeStore;
        private readonly ITokenService _tokenService;
        private readonly IRefreshTokenStore _refreshTokenStore;
        private readonly AppSetting _appSetting;

        public AuthorizationController(
            IClientStore clientStore,
            IAuthorizationCodeStore authCodeStore,
            ITokenService tokenService,
            IRefreshTokenStore refreshTokenStore,
            AppSetting appSetting)
        {
            _clientStore = clientStore;
            _authCodeStore = authCodeStore;
            _tokenService = tokenService;
            _refreshTokenStore = refreshTokenStore;
            _appSetting = appSetting;
        }

        [HttpGet("connect/logout")]
        public IActionResult Logout()
        {
            return View();
        }

        [ActionName(nameof(Logout)), HttpPost("~/connect/logout"), ValidateAntiForgeryToken]
        public async Task<IActionResult> LogoutPost()
        {
            await HttpContext.SignOutAsync();

            var postLogoutRedirectUri = Request.Query["post_logout_redirect_uri"].ToString();
            var clientId = Request.Query["client_id"].ToString();

            if (!string.IsNullOrEmpty(postLogoutRedirectUri) &&
                await IsAllowedPostLogoutRedirectUriAsync(clientId, postLogoutRedirectUri))
            {
                return Redirect(postLogoutRedirectUri);
            }

            return Redirect("/");
        }

        // Only redirect to a same-site relative URL, or a URI registered as a redirect
        // URI for the client that requested the logout, to prevent an open redirect.
        private async Task<bool> IsAllowedPostLogoutRedirectUriAsync(string clientId, string redirectUri)
        {
            if (IsLocalUrl(redirectUri))
            {
                return true;
            }

            if (string.IsNullOrEmpty(clientId))
            {
                return false;
            }

            var client = await _clientStore.FindByClientIdAsync(clientId);
            return client?.RedirectUris != null && client.RedirectUris.Contains(redirectUri);
        }

        public static bool IsLocalUrl(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                return false;
            }

            if (url[0] == '/')
            {
                // Allow "/" or "/foo" but not "//evil.com" or "/\evil.com",
                // which browsers treat as protocol-relative URLs.
                if (url.Length == 1 || (url[1] != '/' && url[1] != '\\'))
                {
                    return true;
                }
            }

            return false;
        }

        [HttpGet("connect/userinfo")]
        [HttpPost("connect/userinfo")]  // OIDC spec allows both GET and POST
        [Authorize]
        public Task<IActionResult> UserInfo()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
            var userName = User.FindFirst(ClaimTypes.Name)?.Value ?? User.Identity.Name;
            var email = User.FindFirst(ClaimTypes.Email)?.Value ?? userName;

            if (string.IsNullOrEmpty(userId))
            {
                return Task.FromResult<IActionResult>(Unauthorized());
            }

            var claims = new Dictionary<string, object>
            {
                ["sub"] = userId,
                ["email"] = email,
                ["name"] = userName,
                ["preferred_username"] = userName
            };

            // Add custom claims
            var thumbprint = User.FindFirst("Thumbprint")?.Value;
            if (!string.IsNullOrEmpty(thumbprint))
            {
                claims["thumbprint"] = thumbprint;
            }

            var sessionId = User.FindFirst("sessionId")?.Value;
            if (!string.IsNullOrEmpty(sessionId))
            {
                claims["sessionId"] = sessionId;
            }

            var roleClaims = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
            if (roleClaims.Any())
            {
                claims["roles"] = roleClaims;
            }

            return Task.FromResult<IActionResult>(Ok(claims));
        }

        [HttpGet(".well-known/openid-configuration")]
        public IActionResult Discovery()
        {
            var baseUrl = $"{_appSetting["IdentityUrlBase"]}/openidc";

            var discoveryDocument = new
            {
                issuer = baseUrl,
                authorization_endpoint = $"{baseUrl}/login",
                token_endpoint = $"{baseUrl}/connect/token",
                userinfo_endpoint = $"{baseUrl}/connect/userinfo",
                jwks_uri = $"{baseUrl}/.well-known/jwks",
                end_session_endpoint = $"{baseUrl}/connect/logout",
                scopes_supported = new[] { "openid", "profile", "email", "roles" },
                response_types_supported = new[] { "code" },
                grant_types_supported = new[] { "authorization_code", "refresh_token" },
                subject_types_supported = new[] { "public" },
                id_token_signing_alg_values_supported = new[] { "RS256" },
                token_endpoint_auth_methods_supported = new[] { "client_secret_post", "client_secret_basic" },
                code_challenge_methods_supported = new[] { "S256", "plain" }
            };

            return Ok(discoveryDocument);
        }

        [HttpGet(".well-known/jwks")]
        public IActionResult Jwks()
        {
            var jwks = _tokenService.GetJsonWebKeySet();
            return Ok(jwks);
        }
    }
}