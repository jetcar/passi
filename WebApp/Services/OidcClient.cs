using ConfigurationManager;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RestSharp;
using Services;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace WebApp.Services
{
    public class OidcClient : IOidcClient
    {
        private readonly AppSetting _appSetting;
        private readonly IMyRestClient _myRestClient;
        private readonly ILogger<OidcClient> _logger;
        private readonly string _clientId;
        private readonly string _clientSecret;
        private readonly string _discoveryUrl;
        private string _authorizationEndpoint;
        private string _tokenEndpoint;

        public OidcClient(
            AppSetting appSetting,
            IMyRestClient myRestClient,
            ILogger<OidcClient> logger)
        {
            _appSetting = appSetting;
            _myRestClient = myRestClient;
            _logger = logger;

            var openIdcUrl = Environment.GetEnvironmentVariable("openIdcUrl") ?? appSetting["openIdcUrl"];
            _clientId = Environment.GetEnvironmentVariable("ClientId") ?? appSetting["ClientId"];
            _clientSecret = Environment.GetEnvironmentVariable("ClientSecret") ?? appSetting["ClientSecret"];

            _discoveryUrl = $"{openIdcUrl}/.well-known/openid-configuration";
        }

        private async Task<Dictionary<string, object>> GetDiscoveryDocumentAsync()
        {
            _logger.LogDebug("Fetching discovery document from: {DiscoveryUrl}", _discoveryUrl);

            var request = new RestRequest(_discoveryUrl, Method.Get);
            var result = await _myRestClient.ExecuteAsync(request);

            if (!result.IsSuccessful)
            {
                _logger.LogError("Failed to fetch discovery document: {Content}", result.Content);
                throw new InvalidOperationException($"Failed to fetch discovery document: {result.Content}");
            }

            return JsonConvert.DeserializeObject<Dictionary<string, object>>(result.Content);
        }

        public async Task<string> BuildAuthorizationUrlAsync(string redirectUri, string returnUrl, string state, string nonce, string codeVerifier)
        {
            // Fetch discovery document to get authorization endpoint
            var discovery = await GetDiscoveryDocumentAsync();
            _authorizationEndpoint = discovery["authorization_endpoint"]?.ToString();
            _tokenEndpoint = discovery["token_endpoint"]?.ToString();

            _logger.LogDebug("Using authorization_endpoint: {Endpoint}", _authorizationEndpoint);

            var codeChallenge = GenerateCodeChallenge(codeVerifier);

            var queryParams = new Dictionary<string, string>
            {
                ["client_id"] = _clientId,
                ["redirect_uri"] = redirectUri,
                ["response_type"] = "code",
                ["scope"] = "openid profile email",
                ["state"] = state,
                ["nonce"] = nonce,
                ["code_challenge"] = codeChallenge,
                ["code_challenge_method"] = "S256"
            };

            var query = string.Join("&", queryParams.Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}"));

            _logger.LogDebug("Built authorization URL with state: {State}, nonce: {Nonce}", state, nonce);

            return $"{_authorizationEndpoint}?{query}";
        }

        public async Task<OidcTokenResponse> ExchangeCodeForTokensAsync(string redirectUri, string code, string codeVerifier)
        {
            // Ensure we have token endpoint from discovery
            if (string.IsNullOrEmpty(_tokenEndpoint))
            {
                var discovery = await GetDiscoveryDocumentAsync();
                _tokenEndpoint = discovery["token_endpoint"]?.ToString();
            }

            _logger.LogInformation("Exchanging authorization code for tokens");

            var request = new RestRequest(_tokenEndpoint, Method.Post);
            request.AddHeader("Content-Type", "application/x-www-form-urlencoded");
            request.AddParameter("grant_type", "authorization_code");
            request.AddParameter("code", code);
            request.AddParameter("redirect_uri", redirectUri);
            request.AddParameter("client_id", _clientId);
            request.AddParameter("code_verifier", codeVerifier);

            var result = await _myRestClient.ExecuteAsync(request);

            _logger.LogDebug("Token exchange response - Success: {Success}, StatusCode: {StatusCode}",
                result.IsSuccessful, result.StatusCode);

            if (!result.IsSuccessful)
            {
                _logger.LogError("Token exchange failed: {Content}", result.Content);
                throw new InvalidOperationException($"Token exchange failed: {result.Content}");
            }

            var tokenResponse = JsonConvert.DeserializeObject<Dictionary<string, object>>(result.Content);

            return new OidcTokenResponse
            {
                AccessToken = tokenResponse["access_token"]?.ToString(),
                IdToken = tokenResponse["id_token"]?.ToString(),
                RefreshToken = tokenResponse.ContainsKey("refresh_token") ? tokenResponse["refresh_token"]?.ToString() : null,
                ExpiresIn = tokenResponse.ContainsKey("expires_in") ? int.Parse(tokenResponse["expires_in"]?.ToString() ?? "3600") : 3600,
                TokenType = tokenResponse.ContainsKey("token_type") ? tokenResponse["token_type"]?.ToString() : "Bearer"
            };
        }

        public async Task<ClaimsPrincipal> ValidateTokensAsync(OidcTokenResponse tokenResponse)
        {
            _logger.LogInformation("Validating and parsing ID token");

            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(tokenResponse.IdToken);

            _logger.LogDebug("ID token parsed successfully. Subject: {Subject}", jwtToken.Subject);

            var claims = jwtToken.Claims.ToList();
            var identity = new ClaimsIdentity(claims, "Bearer");
            var principal = new ClaimsPrincipal(identity);

            // Store tokens in claims for later use
            ((ClaimsIdentity)principal.Identity).AddClaim(new Claim("access_token", tokenResponse.AccessToken));
            ((ClaimsIdentity)principal.Identity).AddClaim(new Claim("id_token", tokenResponse.IdToken));
            if (!string.IsNullOrEmpty(tokenResponse.RefreshToken))
            {
                ((ClaimsIdentity)principal.Identity).AddClaim(new Claim("refresh_token", tokenResponse.RefreshToken));
            }

            return await Task.FromResult(principal);
        }

        private string GenerateCodeVerifier()
        {
            var bytes = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(bytes);
            return Base64UrlEncode(bytes);
        }

        private string GenerateCodeChallenge(string codeVerifier)
        {
            using var sha256 = SHA256.Create();
            var challengeBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(codeVerifier));
            return Base64UrlEncode(challengeBytes);
        }

        private string Base64UrlEncode(byte[] input)
        {
            var base64 = Convert.ToBase64String(input);
            return base64.Replace("+", "-").Replace("/", "_").Replace("=", "");
        }
    }
}
