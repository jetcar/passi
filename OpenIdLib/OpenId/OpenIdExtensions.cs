using System.Net;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OAuth.Claims;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using OpenIdLib.AutomaticTokenManagement;
using RestSharp;
using Services;
using WebApiDto.Certificate;

namespace OpenIdLib.OpenId
{
    public static class OpenIdExtensions
    {
        public static AuthenticationBuilder AddOpenIdAuthentication(this AuthenticationBuilder builder,
            string identityUrl, string returnUrl, string passiUrl, string clientId, string clientSecret)
        {
            ServicePointManager
                    .ServerCertificateValidationCallback +=
                (sender, cert, chain, sslPolicyErrors) => true;

            return builder.AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, options =>
            {
                options.Authority = identityUrl;
                options.ClientId = clientId;
                options.ClientSecret = clientSecret;
                options.Scope.Clear();
                options.Scope.Add("openid email");
                options.ResponseType = "code";
                // options.CorrelationCookie = new CookieBuilder() {SecurePolicy = CookieSecurePolicy.None};
                //options.NonceCookie = new CookieBuilder() {SecurePolicy = CookieSecurePolicy.None};
                options.GetClaimsFromUserInfoEndpoint = true;
                options.SignInScheme = "Cookies";
                options.RequireHttpsMetadata = false;
                options.ClaimActions.Add(new MapAllClaimsAction());
                options.SaveTokens = true;
                HttpClientHandler handler = new HttpClientHandler();

                handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
                options.BackchannelHttpHandler = handler;
                options.ResponseType = OpenIdDefaults.ResponseType;
                options.CallbackPath = new PathString(returnUrl);
                options.SignedOutCallbackPath = new PathString(OpenIdDefaults.SignedOutCallbackPath);

                options.SignedOutRedirectUri = OpenIdDefaults.SignedOutRedirectUri;

                options.Events = new OpenIdConnectEvents
                {
                    OnTokenValidated = context =>
                    {
                        MapOpenIdRolesToRoleClaims(context, passiUrl);
                        return Task.CompletedTask;
                    },

                    OnRedirectToIdentityProvider = context =>
                    {
                        context.ProtocolMessage.RedirectUri = $"https://{context.HttpContext.Request.Host.ToString()}{context.HttpContext.Request.PathBase}{returnUrl}";
                        context.ProtocolMessage.SetParameter("audience", options.ClientId);
                        return Task.FromResult(0);
                    },
                    OnAuthorizationCodeReceived = context =>
                    {
                        Console.WriteLine(context);
                        return Task.FromResult(0);
                    },
                    OnUserInformationReceived = context =>
                    {
                        return Task.FromResult(0);
                    },
                    OnMessageReceived = context =>
                    {
                        return Task.FromResult(0);
                    },
                    OnTicketReceived = context =>
                    {
                        return Task.FromResult(0);
                    },
                    OnTokenResponseReceived = context =>
                    {
                        var Idtoken = context.TokenEndpointResponse.IdToken;
                        return Task.FromResult(0);
                    },
                    OnRedirectToIdentityProviderForSignOut = context =>
                    {
                        return Task.FromResult(0);
                    },
                    OnRemoteSignOut = context =>
                    {
                        return Task.FromResult(0);
                    },
                    OnSignedOutCallbackRedirect = context =>
                    {
                        return Task.FromResult(0);
                    }
                };
            });
        }

        public static AuthenticationBuilder AddOpenIdTokenManagement(
            this AuthenticationBuilder builder,
            Action<AutomaticTokenManagementOptions> configureOptions
        )
        {
            return builder.AddAutomaticTokenManagement(options =>
            {
                ApplyDefaultOpenIdTokenManagementConfiguration(options);
                configureOptions.Invoke(options);
            });
        }

        private static void ApplyDefaultOpenIdTokenManagementConfiguration(
            AutomaticTokenManagementOptions options)
        {
            options.Scheme = OpenIdDefaults.AuthenticationScheme;
        }

        private static void MapOpenIdRolesToRoleClaims(TokenValidatedContext context, string passiUrl)
        {
            // var resourceAccess = JObject.Parse(context.Principal.FindFirst("resource_access").Value);
            // var clientResource = resourceAccess[context.Options.ClientId];
            // var clientRoles = clientResource["roles"];
            var claimsIdentity = context.Principal.Identity as ClaimsIdentity;
            if (claimsIdentity == null)
            {
                return;
            }
            var thumbprint = claimsIdentity.FindFirst("Thumbprint").Value;
            var signedHash = claimsIdentity.FindFirst("SignedHash").Value;
            var username = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (thumbprint != null)
            {
                var client = new RestClient(passiUrl);
                var request = new RestRequest($"api/Certificate/Public?thumbprint={thumbprint}&username={username}", Method.GET);
                var result = client.ExecuteAsync(request).Result;
                if (result.IsSuccessful)
                {
                    var cert = JsonConvert.DeserializeObject<CertificateDto>(result.Content);
                    var publicCertificate = new X509Certificate2(Convert.FromBase64String(cert.PublicCert));
                    var nonce = context.Nonce;

                    // ComputeHash - returns byte array
                    var isValid = CertHelper.VerifyData(nonce, signedHash, cert.PublicCert);

                    if (!isValid)
                        throw new UnauthorizedAccessException("Invalid signature");
                    claimsIdentity.AddClaim(new Claim("PublicCert", cert.PublicCert));
                    claimsIdentity.AddClaim(new Claim("ValidFrom", publicCertificate.NotBefore.ToShortDateString()));
                    claimsIdentity.AddClaim(new Claim("ValidTo", publicCertificate.NotAfter.ToShortDateString()));
                }
            }
        }
    }
}