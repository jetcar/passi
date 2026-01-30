using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace WebApp.Services
{
    public interface IOidcClient
    {
        Task<string> BuildAuthorizationUrlAsync(string redirectUri, string returnUrl, string state, string nonce, string codeVerifier);
        Task<OidcTokenResponse> ExchangeCodeForTokensAsync(string redirectUri, string code, string codeVerifier);
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
