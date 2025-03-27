using ConfigurationManager;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RestSharp;
using Services;
using System;
using System.Collections.Generic;
using System.Net;
using System.Reflection;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using System.Web;
using GoogleTracer;
using Microsoft.AspNetCore;
using OpenIddict.Client.AspNetCore;
using Repos;
using WebApiDto;
using WebApiDto.Auth;
using WebApiDto.Auth.Dto;
using WebApiDto.SignUp;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using static OpenIddict.Abstractions.OpenIddictConstants;
using System.Linq;


[ApiController]
[Route("api")]
[Profile]
public class ApiController : ControllerBase
{
    private AppSetting _appSetting;
    private IMyRestClient _myRestClient;
    private IRandomGenerator _randomGenerator;
    public ILogger<ApiController> Logger { get; set; }
    private readonly SignInManager<IdentityUser> _signInManager;
    private readonly IOpenIddictApplicationManager _applicationManager;
    private readonly IOpenIddictAuthorizationManager _authorizationManager;
    private readonly IOpenIddictScopeManager _scopeManager;

    public ApiController(IRandomGenerator randomGenerator, IMyRestClient myRestClient, AppSetting appSetting, ILogger<ApiController> logger, SignInManager<IdentityUser> signInManager, IOpenIddictApplicationManager applicationManager, IOpenIddictAuthorizationManager authorizationManager, IOpenIddictScopeManager scopeManager)
    {
        ServicePointManager.ServerCertificateValidationCallback +=
            (sender, cert, chain, sslPolicyErrors) => true;
        _randomGenerator = randomGenerator;
        _myRestClient = myRestClient;
        _appSetting = appSetting;
        Logger = logger;
        _signInManager = signInManager;
        _applicationManager = applicationManager;
        _authorizationManager = authorizationManager;
        _scopeManager = scopeManager;
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
    public async Task<IActionResult> Login([FromQuery] string redirect_uri, [FromQuery] string nonce, [FromQuery] string username)
    {

        var context = HttpContext.GetOpenIddictServerRequest();

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
            ClientId = context?.ClientId ?? "IdentityServer",
            ReturnUrl = redirect_uri,
            CheckColor = possibleCodes[index],
            RandomString = nonce ?? _randomGenerator.GetNumbersString(10)
        };
        request.AddJsonBody(startLoginDto);
        var result = await _myRestClient.ExecuteAsync(request);
        if (result.IsSuccessful)
        {
            Logger.LogDebug("response:" + result.Content);
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

        Logger.LogDebug("response:" + result.Content);
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
    public async Task<IActionResult> Check([FromQuery] string sessionId, [FromQuery] string nonce)
    {
        var request = new RestRequest(_appSetting["checkRequest"] + "?sessionId=" + sessionId,
            Method.Get);
        var result = await _myRestClient.ExecuteAsync(request);

        if (result.IsSuccessful)
        {
            var context = HttpContext.GetOpenIddictServerRequest();

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
                        if (isValid)
                        {
                            IdentityUser identityUser = new IdentityUser(checkResponceDto.Username);
                            await _signInManager.SignInAsync(identityUser, new OAuthChallengeProperties());
                            var authResult = await HttpContext.AuthenticateAsync();
                            var application = await _applicationManager.FindByClientIdAsync(context.ClientId) ??
                                              throw new InvalidOperationException("Details concerning the calling client application cannot be found.");


                            var authorizations = await _authorizationManager.FindAsync(
                                subject: identityUser.UserName,
                                client: await _applicationManager.GetIdAsync(application),
                                status: OpenIddictConstants.Statuses.Valid,
                                type: OpenIddictConstants.AuthorizationTypes.Permanent,
                                scopes: context.GetScopes()).ToListAsync();


                            var identity = new ClaimsIdentity(
                                authenticationType: TokenValidationParameters.DefaultAuthenticationType,
                                nameType: OpenIddictConstants.Claims.Name,
                                roleType: OpenIddictConstants.Claims.Role);

                            // Add the claims that will be persisted in the tokens.
                            identity.SetClaim(OpenIddictConstants.Claims.Subject, identityUser.UserName)
                                .SetClaim(OpenIddictConstants.Claims.Email, identityUser.UserName)
                                .SetClaim("Thumbprint", checkResponceDto.PublicCertThumbprint)
                                .SetClaim("sessionId", sessionId)
                                ;


                            identity.SetScopes(context.GetScopes());
                            identity.SetResources(await _scopeManager.ListResourcesAsync(identity.GetScopes()).ToListAsync());

                            // Automatically create a permanent authorization to avoid requiring explicit consent
                            // for future authorization or token requests containing the same scopes.
                            var authorization = authorizations.LastOrDefault();
                            authorization ??= await _authorizationManager.CreateAsync(
                                identity: identity,
                                subject: identityUser.UserName,
                                client: await _applicationManager.GetIdAsync(application),
                                type: AuthorizationTypes.Permanent,
                                scopes: identity.GetScopes());

                            identity.SetAuthorizationId(await _authorizationManager.GetIdAsync(authorization));

                            return SignIn(new ClaimsPrincipal(identity), OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
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
