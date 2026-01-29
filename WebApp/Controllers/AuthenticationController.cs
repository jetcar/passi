using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RestSharp;
using Services;
using WebApiDto.Auth.Dto;
using WebApp.Services;

namespace WebApp.Controllers;

public class AuthenticationController : Controller
{
    private readonly IMyRestClient _myRest;
    private readonly ILogger<AuthenticationController> _logger;
    private readonly IOidcClient _oidcClient;
    private const string CodeVerifierSessionKey = "code_verifier";
    private const string StateSessionKey = "oauth_state";
    private const string NonceSessionKey = "oauth_nonce";
    private const string ReturnUrlSessionKey = "return_url";

    public AuthenticationController(IMyRestClient myRest, ILogger<AuthenticationController> logger, IOidcClient oidcClient)
    {
        _myRest = myRest;
        _logger = logger;
        _oidcClient = oidcClient;
    }

    [HttpGet("~/login")]
    public ActionResult LogIn(string returnUrl)
    {
        _logger.LogInformation("Login initiated - ReturnUrl: {ReturnUrl}", returnUrl);

        // Generate state and nonce for CSRF and replay protection
        var state = GenerateRandomString(32);
        var nonce = GenerateRandomString(32);
        var codeVerifier = GenerateRandomString(43);

        // Store in session for validation during callback
        HttpContext.Session.SetString(StateSessionKey, state);
        HttpContext.Session.SetString(NonceSessionKey, nonce);
        HttpContext.Session.SetString(CodeVerifierSessionKey, codeVerifier);
        HttpContext.Session.SetString(ReturnUrlSessionKey, Url.IsLocalUrl(returnUrl) ? returnUrl : "/");

        var authorizationUrl = _oidcClient.BuildAuthorizationUrl(returnUrl, state, nonce);

        _logger.LogDebug("Redirecting to authorization endpoint");

        return Redirect(authorizationUrl);
    }

    private string GenerateRandomString(int length)
    {
        var bytes = new byte[length];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").Replace("=", "").Substring(0, length);
    }

    [HttpPost("~/logout"), ValidateAntiForgeryToken]
    public async Task<ActionResult> LogOut(string returnUrl)
    {
        _logger.LogInformation("Logout initiated");

        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        return Redirect(Url.IsLocalUrl(returnUrl) ? returnUrl : "/");
    }

    [HttpGet("~/callback/login/{provider}"), IgnoreAntiforgeryToken]
    public async Task<ActionResult> LogInCallback(string code, string state)
    {
        _logger.LogInformation("Login callback received - Code: {Code}, State: {State}",
            code?.Substring(0, Math.Min(8, code?.Length ?? 0)) + "...", state);

        // Validate state to prevent CSRF
        var expectedState = HttpContext.Session.GetString(StateSessionKey);
        if (string.IsNullOrEmpty(expectedState) || expectedState != state)
        {
            _logger.LogError("State mismatch or missing. Expected: {Expected}, Got: {Got}", expectedState, state);
            throw new InvalidOperationException("Invalid state parameter");
        }

        var nonce = HttpContext.Session.GetString(NonceSessionKey);
        var codeVerifier = HttpContext.Session.GetString(CodeVerifierSessionKey);
        var returnUrl = HttpContext.Session.GetString(ReturnUrlSessionKey) ?? "/";

        if (string.IsNullOrEmpty(code))
        {
            _logger.LogError("Authorization code is missing");
            throw new InvalidOperationException("Authorization code is required");
        }

        // Exchange code for tokens
        var tokenResponse = await _oidcClient.ExchangeCodeForTokensAsync(code, codeVerifier);
        var principal = await _oidcClient.ValidateTokensAsync(tokenResponse);

        _logger.LogDebug("Tokens validated, building claims identity");

        // Build identity with claims from ID token
        var identity = new ClaimsIdentity(CookieAuthenticationDefaults.AuthenticationScheme);

        var username = principal.FindFirst("sub")?.Value ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var thumbprint = principal.FindFirst("Thumbprint")?.Value;
        var sessionId = principal.FindFirst("sessionId")?.Value;

        _logger.LogInformation("Processing claims - Username: {Username}, Thumbprint: {Thumbprint}, SessionId: {SessionId}",
            username, thumbprint?.Substring(0, Math.Min(8, thumbprint?.Length ?? 0)), sessionId);

        if (!string.IsNullOrEmpty(thumbprint))
        {
            _logger.LogDebug("Validating session and certificate");
            var request = new RestRequest($"api/Auth/session?sessionId={sessionId}&thumbprint={thumbprint}&username={username}", Method.Get);
            var requestResult = await _myRest.ExecuteAsync(request);

            _logger.LogDebug("Session validation response - Success: {Success}, StatusCode: {StatusCode}",
                requestResult.IsSuccessful, requestResult.StatusCode);

            if (requestResult.IsSuccessful)
            {
                var cert = JsonConvert.DeserializeObject<SessionMinDto>(requestResult.Content);
                var publicCertificate = new X509Certificate2(Convert.FromBase64String(cert.PublicCert));

                // Verify signature
                var isValid = CertHelper.VerifyData(nonce, cert.SignedHash, cert.PublicCert);

                _logger.LogInformation("Signature validation result: {IsValid}", isValid);

                if (!isValid)
                {
                    _logger.LogError("Invalid signature for user: {Username}", username);
                    throw new UnauthorizedAccessException("Invalid signature");
                }

                identity.AddClaim(new Claim("PublicCert", cert.PublicCert));
                identity.AddClaim(new Claim("ValidFrom", publicCertificate.NotBefore.ToShortDateString()));
                identity.AddClaim(new Claim("ValidTo", publicCertificate.NotAfter.ToShortDateString()));
            }
        }
        else
        {
            _logger.LogError("Missing thumbprint claim for user: {Username}", username);
            throw new UnauthorizedAccessException("Missing thumbprint");
        }

        // Add all claims from the principal
        foreach (var claim in principal.Claims)
        {
            identity.AddClaim(claim);
        }

        // Add standard claims
        if (principal.FindFirst(ClaimTypes.Email) != null)
            identity.AddClaim(new Claim(ClaimTypes.Email, principal.FindFirst(ClaimTypes.Email).Value));
        if (principal.FindFirst(ClaimTypes.Name) != null)
            identity.AddClaim(new Claim(ClaimTypes.Name, principal.FindFirst(ClaimTypes.Name).Value));
        if (principal.FindFirst(ClaimTypes.NameIdentifier) == null && !string.IsNullOrEmpty(username))
            identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, username));

        // Store tokens
        var authProperties = new AuthenticationProperties
        {
            IsPersistent = true,
            ExpiresUtc = DateTimeOffset.UtcNow.AddSeconds(tokenResponse.ExpiresIn),
            RedirectUri = returnUrl
        };

        authProperties.StoreTokens(new[]
        {
            new AuthenticationToken { Name = "access_token", Value = tokenResponse.AccessToken },
            new AuthenticationToken { Name = "id_token", Value = tokenResponse.IdToken },
            new AuthenticationToken { Name = "refresh_token", Value = tokenResponse.RefreshToken ?? "" }
        });

        // Clear session
        HttpContext.Session.Remove(StateSessionKey);
        HttpContext.Session.Remove(NonceSessionKey);
        HttpContext.Session.Remove(CodeVerifierSessionKey);
        HttpContext.Session.Remove(ReturnUrlSessionKey);

        _logger.LogInformation("Login successful for user: {Username}, redirecting to: {RedirectUri}",
            username, returnUrl);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            authProperties);

        return Redirect(returnUrl);
    }

    [HttpGet("~/callback/logout/{provider}"), IgnoreAntiforgeryToken]
    public ActionResult LogOutCallback()
    {
        _logger.LogInformation("Logout callback received");
        return Redirect("/");
    }
}
