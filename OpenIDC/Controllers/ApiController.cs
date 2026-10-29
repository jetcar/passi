using ConfigurationManager;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RestSharp;
using Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using GoogleTracer;
using WebApiDto;
using WebApiDto.Auth;
using WebApiDto.Auth.Dto;
using OpenIDC.Models;
using OpenIDC.Services;
using OpenIDC.Helpers;
using System.Net;


[ApiController]
[Route("api")]
[Profile]
[IgnoreAntiforgeryToken]
public class ApiController : ControllerBase
{
    // Test seam: lets tests observe that the certificate loaded here is disposed, without
    // changing Check's public contract (it still just reads the PEM-encoded cert string).
    internal static Func<string, X509Certificate2> LoadCertificateFromPem = pem => X509Certificate2.CreateFromPem(pem);

    private readonly AppSetting _appSetting;
    private readonly IMyRestClient _myRestClient;
    private readonly IRandomGenerator _randomGenerator;
    private readonly ILogger<ApiController> _logger;
    private readonly IClientStore _clientStore;
    private readonly IAuthorizationCodeStore _authCodeStore;
    private readonly ILoginRateLimiter _loginRateLimiter;

    public ApiController(
        IRandomGenerator randomGenerator,
        IMyRestClient myRestClient,
        AppSetting appSetting,
        ILogger<ApiController> logger,
        IClientStore clientStore,
        IAuthorizationCodeStore authCodeStore,
        ILoginRateLimiter loginRateLimiter)
    {
        _randomGenerator = randomGenerator;
        _myRestClient = myRestClient;
        _appSetting = appSetting;
        _logger = logger;
        _clientStore = clientStore;
        _authCodeStore = authCodeStore;
        _loginRateLimiter = loginRateLimiter;
    }




    [HttpGet]
    [Route("userloggedin")]
    public UserDto UserLoggedIn()
    {
        return new UserDto()
        {
            IsLoggedIn = HttpContext.User.Identity.IsAuthenticated,
            Name = HttpContext.User.Identity.Name,
        };
    }

    [HttpPost]
    [HttpGet]
    [Route("login")]
    public async Task<IActionResult> Login(
        [FromQuery] string redirect_uri,
        [FromQuery] string nonce,
        [FromQuery] string username,
        [FromQuery] string client_id,
        [FromQuery] string response_type,
        [FromQuery] string scope,
        [FromQuery] string state,
        [FromQuery] string code_challenge,
        [FromQuery] string code_challenge_method)
    {
        var authorizationError = await ValidateAuthorizationRequest(
            client_id, redirect_uri, response_type, scope, state, code_challenge, code_challenge_method);
        if (authorizationError != null)
            return authorizationError;

        try
        {
            if (!_loginRateLimiter.TryAcquire(HttpContext, username, out var retryAfter))
            {
                Response.Headers.RetryAfter = Math.Max(1, (int)Math.Ceiling(retryAfter.TotalSeconds))
                    .ToString(System.Globalization.CultureInfo.InvariantCulture);
                return AuthorizationError(
                    "temporarily_unavailable",
                    "Too many login attempts. Please try again later.",
                    redirect_uri,
                    state,
                    StatusCodes.Status429TooManyRequests);
            }
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Login rate limiter failed.");
            return AuthorizationError(
                "temporarily_unavailable",
                "Login is temporarily unavailable.",
                redirect_uri,
                state,
                StatusCodes.Status503ServiceUnavailable);
        }

        var request = new RestRequest(_appSetting["startRequest"], Method.Post);
        var possibleCodes = new List<Color>()
        {
            Color.blue,
            Color.green,
            Color.red,
            Color.yellow
        };
        var index = new Random().Next(0, possibleCodes.Count);
        var startLoginDto = new StartLoginDto()
        {
            Username = username,
            ClientId = client_id ?? "SampleApp",
            ReturnUrl = redirect_uri,
            CheckColor = possibleCodes[index],
            CheckNumber = System.Security.Cryptography.RandomNumberGenerator.GetInt32(10, 100),
            RandomString = nonce ?? _randomGenerator.GetNumbersString(10)
        };
        request.AddJsonBody(startLoginDto);
        var result = await _myRestClient.ExecuteAsync(request);
        if (result.IsSuccessful)
        {
            _logger.LogDebug("response:" + result.Content);
            var loginResponceDto = JsonConvert.DeserializeObject<LoginResponceDto>(result.Content);
            if (loginResponceDto == null)
            {
                _logger.LogWarning("Login start request returned an empty response.");
                return BadRequest(new ApiResponseDto() { errors = "Internal error" });
            }

            var color = startLoginDto.CheckColor.ToString();
            return Ok(new CheckInputModel
            {
                SessionId = loginResponceDto.SessionId,
                ReturnUrl = redirect_uri,
                Username = username,
                CheckColor = color,
                CheckNumber = startLoginDto.CheckNumber,
                RandomString = startLoginDto.RandomString,
                RegisteredDevices = loginResponceDto.RegisteredDevices ?? []
            });
        }

        _logger.LogDebug("response:" + result.Content);
        if (result.Content != null)
        {
            return BadRequest(new ApiResponseDto() { errors = GetApiErrorMessage(result.Content) });
        }

        return BadRequest(new ApiResponseDto() { errors = "Internal error" });
    }

    [HttpGet]
    [Route("authorize")]
    public async Task<IActionResult> Authorize(
        [FromQuery] string client_id,
        [FromQuery] string redirect_uri,
        [FromQuery] string response_type,
        [FromQuery] string scope,
        [FromQuery] string state,
        [FromQuery] string code_challenge,
        [FromQuery] string code_challenge_method)
    {
        var authorizationError = await ValidateAuthorizationRequest(
            client_id, redirect_uri, response_type, scope, state, code_challenge, code_challenge_method);
        return authorizationError ?? Ok(new { valid = true });
    }

    private async Task<IActionResult> ValidateAuthorizationRequest(
        string clientId,
        string redirectUri,
        string responseType,
        string scope,
        string state,
        string codeChallenge,
        string codeChallengeMethod)
    {
        if (string.IsNullOrWhiteSpace(clientId))
            return AuthorizationError("invalid_request", "client_id is required", null, state);

        var client = await _clientStore.GetClientAsync(clientId);
        if (client == null)
            return AuthorizationError("invalid_request", "Invalid client_id", null, state);

        if (!RedirectUriHelper.IsAllowed(redirectUri, client))
            return AuthorizationError("invalid_request", "Invalid redirect_uri", null, state);

        if (string.IsNullOrWhiteSpace(responseType))
            return AuthorizationError("invalid_request", "response_type is required", redirectUri, state);

        if (!string.Equals(responseType, "code", StringComparison.Ordinal))
            return AuthorizationError("unsupported_response_type", "response_type must be code", redirectUri, state);

        var requestedScopes = scope?.Split(' ', StringSplitOptions.RemoveEmptyEntries) ?? [];
        if (requestedScopes.Length == 0 || !requestedScopes.Contains("openid", StringComparer.Ordinal))
            return AuthorizationError("invalid_scope", "scope must include openid", redirectUri, state);

        var allowedScopes = client.AllowedScopes ?? [];
        var unsupportedScope = requestedScopes.FirstOrDefault(requested =>
            !allowedScopes.Contains(requested, StringComparer.Ordinal));
        if (unsupportedScope != null)
            return AuthorizationError("invalid_scope", $"Unsupported scope: {unsupportedScope}", redirectUri, state);

        if (client.RequiresPkce && string.IsNullOrWhiteSpace(codeChallenge))
            return AuthorizationError("invalid_request", "PKCE code_challenge is required", redirectUri, state);

        if (string.IsNullOrWhiteSpace(codeChallenge) && !string.IsNullOrWhiteSpace(codeChallengeMethod))
            return AuthorizationError("invalid_request", "code_challenge_method requires code_challenge", redirectUri, state);

        if (!string.IsNullOrWhiteSpace(codeChallenge) &&
            !string.IsNullOrWhiteSpace(codeChallengeMethod) &&
            codeChallengeMethod != "S256" && codeChallengeMethod != "plain")
            return AuthorizationError("invalid_request", "Unsupported code_challenge_method", redirectUri, state);

        return null;
    }

    private IActionResult AuthorizationError(
        string error,
        string description,
        string redirectUri,
        string state,
        int statusCode = StatusCodes.Status400BadRequest)
    {
        _logger.LogWarning("Authorization request rejected: {Error}. {Description}", error, description);
        var payload = new
        {
            error,
            error_description = description,
            errors = description,
            redirect_url = string.IsNullOrEmpty(redirectUri)
                ? null
                : RedirectUriHelper.BuildAuthorizationErrorResponse(redirectUri, error, description, state),
        };
        return statusCode == StatusCodes.Status400BadRequest
            ? BadRequest(payload)
            : StatusCode(statusCode, payload);
    }

    private static string GetApiErrorMessage(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return "Internal error";
        }

        try
        {
            var errorResult = JsonConvert.DeserializeObject<ApiResponseDto>(content);
            if (!string.IsNullOrWhiteSpace(errorResult?.errors))
            {
                return errorResult.errors;
            }
        }
        catch (JsonException)
        {
        }

        try
        {
            var errorMessage = JsonConvert.DeserializeObject<string>(content);
            if (!string.IsNullOrWhiteSpace(errorMessage))
            {
                return errorMessage;
            }
        }
        catch (JsonException)
        {
        }

        return WebUtility.HtmlDecode(content).Trim().Trim('"');
    }

    [HttpPost]
    [HttpGet]
    [Route("check")]
    public async Task<IActionResult> Check(
        [FromQuery] string sessionId,
        [FromQuery] string nonce,
        [FromQuery] string client_id,
        [FromQuery] string redirect_uri,
        [FromQuery] string scope,
        [FromQuery] string state,
        [FromQuery] string code_challenge,
        [FromQuery] string code_challenge_method)
    {
        _logger.LogInformation("Check endpoint called - SessionId: {SessionId}, ClientId: {ClientId}, RedirectUri: {RedirectUri}",
            sessionId, client_id, redirect_uri);

        // Validate client and redirect_uri BEFORE processing authentication
        var client = await _clientStore.GetClientAsync(client_id);
        if (client == null)
        {
            _logger.LogWarning("Client not found: {ClientId}", client_id);
            return BadRequest(new ApiResponseDto() { errors = "Invalid client_id" });
        }

        // Validate redirect_uri against registered URIs (exact match including protocol)
        if (!RedirectUriHelper.IsAllowed(redirect_uri, client))
        {
            _logger.LogWarning("Invalid redirect_uri: {RedirectUri} for client: {ClientId}. Registered URIs: {RegisteredUris}",
                redirect_uri, client_id, string.Join(", ", client.RedirectUris ?? new List<string>()));
            return BadRequest(new ApiResponseDto() { errors = "Invalid redirect_uri - must exactly match registered URI including protocol (http/https)" });
        }

        var request = new RestRequest(_appSetting["checkRequest"] + "?sessionId=" + sessionId,
            Method.Get);
        var result = await _myRestClient.ExecuteAsync(request);

        _logger.LogDebug("Check request response - Success: {Success}, StatusCode: {StatusCode}",
            result.IsSuccessful, result.StatusCode);

        if (result.IsSuccessful)
        {
            var checkResponceDto = JsonConvert.DeserializeObject<CheckResponceDto>(result.Content);

            if (checkResponceDto != null && checkResponceDto.PublicCertThumbprint != null)
            {
                var request2 =
                    new RestRequest(
                        $"api/Certificate/Public?thumbprint={checkResponceDto.PublicCertThumbprint}&username={checkResponceDto.Username}",
                        Method.Get);
                var result2 = await _myRestClient.ExecuteAsync(request2);
                if (result2.IsSuccessful)
                {
                    var cert = JsonConvert.DeserializeObject<CertificateDto>(result2.Content);
                    if (cert?.PublicCert == null)
                    {
                        _logger.LogWarning("Certificate lookup returned no certificate for user: {Username}", checkResponceDto.Username);
                        return BadRequest(new ApiResponseDto() { errors = "Invalid Certificate" });
                    }

                    using var publicCertificate = LoadCertificateFromPem($"-----BEGIN CERTIFICATE-----\r\n{cert.PublicCert}\r\n-----END CERTIFICATE-----");
                    if (publicCertificate.NotAfter < DateTime.UtcNow ||
                        publicCertificate.NotBefore > DateTime.UtcNow)
                    {
                        _logger.LogWarning("Certificate is not currently valid for user: {Username}", checkResponceDto.Username);
                        return BadRequest(new ApiResponseDto() { errors = "Invalid Certificate" });
                    }

                    var requestCurrentSessionReq =
                        new RestRequest(
                            $"api/Auth/session?sessionId={sessionId}&thumbprint={checkResponceDto.PublicCertThumbprint}&username={checkResponceDto.Username}",
                            Method.Get);
                    var requestCurrentSessionResult = await _myRestClient.ExecuteAsync(requestCurrentSessionReq);

                    _logger.LogDebug("Current session request - Success: {Success}, Content: {Content}",
                        requestCurrentSessionResult.IsSuccessful, requestCurrentSessionResult.Content);

                    if (requestCurrentSessionResult.IsSuccessful)
                    {
                        var session =
                            JsonConvert.DeserializeObject<SessionMinDto>(requestCurrentSessionResult.Content);

                        // Verify against the session's server-stored random string: that is what the phone signed (the
                        // client's nonce, or a generated value when the client sent none, e.g. MCP agents). Never trust
                        // a browser-supplied value here.
                        var isValid = session != null && !string.IsNullOrEmpty(session.RandomString) && !string.IsNullOrEmpty(session.SignedHash) &&
                                      CertHelper.VerifyData(session.RandomString, session.SignedHash, cert.PublicCert);

                        _logger.LogDebug("Signature validation result: {IsValid}, Username: {Username}",
                            isValid, checkResponceDto.Username);

                        if (isValid)
                        {
                            // Create authorization code using new OIDC services
                            List<string> scopes = !string.IsNullOrEmpty(scope)
                                ? [.. scope.Split(' ')]
                                : ["openid", "profile", "email"];

                            var authCode = new AuthorizationCode
                            {
                                ClientId = client_id,
                                Subject = checkResponceDto.Username,
                                RedirectUri = redirect_uri,
                                Scopes = scopes,
                                CodeChallenge = code_challenge,
                                CodeChallengeMethod = code_challenge_method ?? "S256",
                                Nonce = nonce,
                                Claims = new Dictionary<string, string>
                                {
                                    ["email"] = checkResponceDto.Username,
                                    ["name"] = checkResponceDto.Username,
                                    ["preferred_username"] = checkResponceDto.Username,
                                    ["Thumbprint"] = checkResponceDto.PublicCertThumbprint,
                                    ["sessionId"] = sessionId
                                }
                            };

                            var code = await _authCodeStore.CreateAuthorizationCodeAsync(authCode);

                            _logger.LogInformation("Authorization code created: {Code}, redirecting to: {RedirectUri}",
                                code.Substring(0, Math.Min(8, code.Length)) + "...", redirect_uri);

                            // Return authorization code to redirect
                            var redirectUrl = RedirectUriHelper.BuildAuthorizationResponse(redirect_uri, code, state);

                            return Ok(new { redirect_url = redirectUrl });
                        }
                        else
                        {
                            return BadRequest(new ApiResponseDto() { errors = "Invalid Signature" });
                        }
                    }
                }
                else
                {
                    return BadRequest(new ApiResponseDto()
                    { errors = result2.Content + " " + result2.ErrorMessage });
                }
            }
        }

        if (result.Content == null)
        {
            return Ok(new { Continue = true });
        }

        if (result.Content != null && !result.Content.Contains("Waiting for response"))
        {
            return BadRequest(new ApiResponseDto() { errors = result.Content });
        }

        return Ok(new { Continue = true });
    }


}

public class UserDto
{
    public bool IsLoggedIn { get; set; }
    public string Name { get; set; }
}
