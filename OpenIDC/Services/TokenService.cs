using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.IdentityModel.Tokens;
using ConfigurationManager;

namespace OpenIDC.Services
{
    public interface ITokenService
    {
        string GenerateAccessToken(string subject, string clientId, List<string> scopes, Dictionary<string, string> claims);
        string GenerateIdToken(string subject, string clientId, List<string> scopes, Dictionary<string, string> claims, string nonce);
        ClaimsPrincipal ValidateToken(string token);
        ClaimsPrincipal ValidateIdToken(string token, string expectedClientId);
        object GetJsonWebKeySet();
    }

    public class TokenService : ITokenService
    {
        private readonly SigningCredentials _signingCredentials;
        private readonly string _issuer;
        private readonly int _accessTokenLifetimeMinutes = 60;

        public TokenService(AppSetting appSetting, SigningCredentials signingCredentials)
        {
            _signingCredentials = signingCredentials;
            _issuer = $"{appSetting["IdentityUrlBase"]}/openidc";
        }

        public string GenerateAccessToken(string subject, string clientId, List<string> scopes, Dictionary<string, string> claims)
        {
            var tokenClaims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, subject),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim("client_id", clientId),
                new Claim("scope", string.Join(" ", scopes))
            };

            foreach (var claim in claims)
            {
                tokenClaims.Add(new Claim(claim.Key, claim.Value));
            }

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(tokenClaims),
                Expires = DateTime.UtcNow.AddMinutes(_accessTokenLifetimeMinutes),
                Issuer = _issuer,
                Audience = clientId,
                SigningCredentials = _signingCredentials
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        public string GenerateIdToken(string subject, string clientId, List<string> scopes, Dictionary<string, string> claims, string nonce)
        {
            var tokenClaims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, subject),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
                new Claim(JwtRegisteredClaimNames.Aud, clientId),
                // Add azp (authorized party) claim - OIDC spec requires it when there are multiple audiences
                // Including it for single-audience tokens is allowed and improves compatibility
                new Claim(JwtRegisteredClaimNames.Azp, clientId)
            };

            if (!string.IsNullOrEmpty(nonce))
            {
                tokenClaims.Add(new Claim("nonce", nonce));
            }

            // Add standard claims based on scopes
            if (scopes.Contains("email") && claims.ContainsKey("email"))
            {
                tokenClaims.Add(new Claim(JwtRegisteredClaimNames.Email, claims["email"]));
            }

            if (scopes.Contains("profile"))
            {
                if (claims.ContainsKey("name"))
                    tokenClaims.Add(new Claim(JwtRegisteredClaimNames.Name, claims["name"]));
                if (claims.ContainsKey("preferred_username"))
                    tokenClaims.Add(new Claim("preferred_username", claims["preferred_username"]));
            }

            // Add custom claims
            foreach (var claim in claims.Where(c => c.Key != "email" && c.Key != "name" && c.Key != "preferred_username"))
            {
                tokenClaims.Add(new Claim(claim.Key, claim.Value));
            }

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(tokenClaims),
                Expires = DateTime.UtcNow.AddMinutes(_accessTokenLifetimeMinutes),
                Issuer = _issuer,
                Audience = clientId,
                SigningCredentials = _signingCredentials
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        public ClaimsPrincipal ValidateToken(string token)
        {
            var tokenHandler = new JwtSecurityTokenHandler();

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = _issuer,
                ValidateAudience = false,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = _signingCredentials.Key
            };

            try
            {
                var principal = tokenHandler.ValidateToken(token, validationParameters, out _);
                return principal;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Validates an ID token according to OIDC spec, including proper azp claim validation.
        /// Per OIDC spec section 3.1.3.7:
        /// - azp is REQUIRED when the ID token has multiple audiences (multiple aud values)
        /// - azp is OPTIONAL for single-audience tokens
        /// - When azp is present, it must match the client_id to which the token was issued
        /// </summary>
        public ClaimsPrincipal ValidateIdToken(string token, string expectedClientId)
        {
            var tokenHandler = new JwtSecurityTokenHandler();

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = _issuer,
                ValidateAudience = true,
                ValidAudience = expectedClientId,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = _signingCredentials.Key
            };

            try
            {
                var principal = tokenHandler.ValidateToken(token, validationParameters, out SecurityToken validatedToken);

                // Additional azp validation per OIDC spec
                var jwtToken = validatedToken as JwtSecurityToken;
                if (jwtToken != null)
                {
                    // Get all audience claims
                    var audiences = jwtToken.Audiences?.ToList() ?? new List<string>();

                    // Get azp claim if present
                    var azpClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Azp);

                    // Per OIDC spec: azp is REQUIRED when there are multiple audiences
                    if (audiences.Count > 1 && azpClaim == null)
                    {
                        throw new SecurityTokenValidationException(
                            "ID token has multiple audiences but missing required 'azp' claim");
                    }

                    // If azp is present, it must match the expected client_id
                    if (azpClaim != null && azpClaim.Value != expectedClientId)
                    {
                        throw new SecurityTokenValidationException(
                            $"ID token 'azp' claim '{azpClaim.Value}' does not match expected client_id '{expectedClientId}'");
                    }
                }

                return principal;
            }
            catch (Exception ex)
            {
                // Log or handle validation errors as needed
                throw new SecurityTokenValidationException($"ID token validation failed: {ex.Message}", ex);
            }
        }

        public object GetJsonWebKeySet()
        {
            RSAParameters parameters;

            // Handle both X509SecurityKey and RsaSecurityKey
            if (_signingCredentials.Key is X509SecurityKey x509Key)
            {
                var rsa = x509Key.Certificate.GetRSAPublicKey()
                    ?? throw new InvalidOperationException("Certificate does not contain an RSA key");
                parameters = rsa.ExportParameters(false);
            }
            else if (_signingCredentials.Key is RsaSecurityKey rsaKey)
            {
                parameters = rsaKey.Rsa?.ExportParameters(false) ?? rsaKey.Parameters;
            }
            else
            {
                throw new InvalidOperationException($"Unsupported key type: {_signingCredentials.Key.GetType().Name}");
            }

            var jwk = new
            {
                kty = "RSA",
                use = "sig",
                kid = _signingCredentials.Kid ?? "default",
                alg = "RS256",
                n = Base64UrlEncoder.Encode(parameters.Modulus),
                e = Base64UrlEncoder.Encode(parameters.Exponent)
            };

            return new
            {
                keys = new[] { jwk }
            };
        }
    }
}
