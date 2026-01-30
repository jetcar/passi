using ConfigurationManager;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RestSharp;
using Services;
using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using GoogleTracer;
using WebApiDto;
using WebApiDto.Auth;
using WebApiDto.Auth.Dto;
using OpenIDC.Models;
using OpenIDC.Services;


[ApiController]
[Route("api")]
[Profile]
public class ApiController : ControllerBase
{
    private readonly AppSetting _appSetting;
    private readonly IMyRestClient _myRestClient;
    private readonly IRandomGenerator _randomGenerator;
    private readonly ILogger<ApiController> _logger;
    private readonly IClientStore _clientStore;
    private readonly IAuthorizationCodeStore _authCodeStore;

    public ApiController(
        IRandomGenerator randomGenerator,
        IMyRestClient myRestClient,
        AppSetting appSetting,
        ILogger<ApiController> logger,
        IClientStore clientStore,
        IAuthorizationCodeStore authCodeStore)
    {
        _randomGenerator = randomGenerator;
        _myRestClient = myRestClient;
        _appSetting = appSetting;
        _logger = logger;
        _clientStore = clientStore;
        _authCodeStore = authCodeStore;
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
    public async Task<IActionResult> Login([FromQuery] string redirect_uri, [FromQuery] string nonce, [FromQuery] string username, [FromQuery] string client_id)
    {
        var request = new RestRequest(_appSetting["startRequest"], Method.Post);
        var possibleCodes = new List<Color>()
        {
            Color.blue,
            Color.green,
            Color.red,
            Color.yellow
        };
        var index = new Random().Next(0, possibleCodes.Count - 1);
        var startLoginDto = new StartLoginDto()
        {
            Username = username,
            ClientId = client_id ?? "SampleApp",
            ReturnUrl = redirect_uri,
            CheckColor = possibleCodes[index],
            RandomString = nonce ?? _randomGenerator.GetNumbersString(10)
        };
        request.AddJsonBody(startLoginDto);
        var result = await _myRestClient.ExecuteAsync(request);
        if (result.IsSuccessful)
        {
            _logger.LogDebug("response:" + result.Content);
            var loginResponceDto = JsonConvert.DeserializeObject<LoginResponceDto>(result.Content);
            var color = startLoginDto.CheckColor.ToString();
            return Ok(new CheckInputModel
            {
                SessionId = loginResponceDto.SessionId,
                ReturnUrl = redirect_uri,
                Username = username,
                CheckColor = color,
                RandomString = startLoginDto.RandomString
            });
        }

        _logger.LogDebug("response:" + result.Content);
        if (result.Content != null)
        {
            var errorResult = JsonConvert.DeserializeObject<ApiResponseDto>(result.Content);

            return BadRequest(new ApiResponseDto() { errors = errorResult?.errors });
        }

        return BadRequest(new ApiResponseDto() { errors = "Internal error" });
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

        var request = new RestRequest(_appSetting["checkRequest"] + "?sessionId=" + sessionId,
            Method.Get);
        var result = await _myRestClient.ExecuteAsync(request);

        _logger.LogDebug("Check request response - Success: {Success}, StatusCode: {StatusCode}",
            result.IsSuccessful, result.StatusCode);

        if (result.IsSuccessful)
        {
            var checkResponceDto = JsonConvert.DeserializeObject<CheckResponceDto>(result.Content);

            if (checkResponceDto.PublicCertThumbprint != null)
            {
                var request2 =
                    new RestRequest(
                        $"api/Certificate/Public?thumbprint={checkResponceDto.PublicCertThumbprint}&username={checkResponceDto.Username}",
                        Method.Get);
                var result2 = await _myRestClient.ExecuteAsync(request2);
                if (result2.IsSuccessful)
                {
                    var cert = JsonConvert.DeserializeObject<CertificateDto>(result2.Content);
                    var publicCertificate = X509Certificate2.CreateFromPem($"-----BEGIN CERTIFICATE-----\r\n{cert.PublicCert}\r\n-----END CERTIFICATE-----");
                    if (publicCertificate.NotAfter < DateTime.UtcNow &&
                        publicCertificate.NotBefore > DateTime.UtcNow)
                    {
                        ModelState.AddModelError("Error", "Invalid Certificate");
                    }

                    var requestCurrentSessionReq =
                        new RestRequest(
                            $"api/Auth/session?sessionId={sessionId}&thumbprint={checkResponceDto.PublicCertThumbprint}&username={checkResponceDto.Username}",
                            Method.Get);
                    var requestCurrentSessionResult = await _myRestClient.ExecuteAsync(requestCurrentSessionReq);
                    if (requestCurrentSessionResult.IsSuccessful)
                    {
                        var session =
                            JsonConvert.DeserializeObject<SessionMinDto>(requestCurrentSessionResult.Content);

                        // ComputeHash - returns byte array
                        var isValid = CertHelper.VerifyData(nonce, session.SignedHash,
                            cert.PublicCert);

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
                            var redirectUrl = $"{redirect_uri}?code={code}&state={state}";

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
