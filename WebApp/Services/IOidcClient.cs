using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace WebApp.Services
{
    public interface IOidcClient
    {
        string BuildAuthorizationUrl(string returnUrl, string state, string nonce);
        Task<OidcTokenResponse> ExchangeCodeForTokensAsync(string code, string codeVerifier);
        Task<ClaimsPrincipal> ValidateTokensAsync(OidcTokenResponse tokenResponse);
    }

    public class OidcTokenResponse
    {
        public string AccessToken { get; set; }
        public string IdToken { get; set; }
        public string RefreshToken { get; set; }
        public int ExpiresIn { get; set; }
        public string TokenType { get; set; }
        public Dictionary<string, string> Claims { get; set; }
    }
}
