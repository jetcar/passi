using ConfigurationManager;
using IdentityServer.Controllers.ViewInputs;
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
using OpenIddict.Abstractions;
using Repos;
using WebApiDto;
using WebApiDto.Auth;
using WebApiDto.Auth.Dto;
using WebApiDto.SignUp;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Server.AspNetCore;
using static OpenIddict.Abstractions.OpenIddictConstants;
using AspNet.Security.OpenId.Steam;
using ServiceStack.Script;
using System.Linq;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OAuth;
using OpenIddict.Client.AspNetCore;
using Microsoft.AspNetCore.Authorization;
using OpenIddict.Validation.AspNetCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace IdentityServer.Controllers
{
    [ApiController]
    [Route("api")]
    [Profile]
    public class ApiController : ControllerBase
    {
        private AppSetting _appSetting;
        private IMyRestClient _myRestClient;
        private IRandomGenerator _randomGenerator;
        private readonly IOpenIddictApplicationManager _applicationManager;
        private OpenIddict.Abstractions.IOpenIddictScopeManager _manager;
        public ILogger<ApiController> Logger { get; set; }
        private readonly SignInManager<IdentityUser> _signInManager;
        public ApiController(IRandomGenerator randomGenerator, IMyRestClient myRestClient, AppSetting appSetting, ILogger<ApiController> logger, IOpenIddictApplicationManager applicationManager)
        {
            ServicePointManager.ServerCertificateValidationCallback +=
                (sender, cert, chain, sslPolicyErrors) => true;
            _randomGenerator = randomGenerator;
            _myRestClient = myRestClient;
            _appSetting = appSetting;
            Logger = logger;
            _applicationManager = applicationManager;
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
        public async Task<IActionResult> Login(LoginInputDto model)
        {
            //var resultAddict = HttpContext.GetOpenIddictServerRequest() ??
            //  throw new InvalidOperationException("The OpenID Connect request cannot be retrieved.");

            //var iddictRequest = HttpContext.GetOpenIddictClientRequest();
            //if (iddictRequest.IsClientCredentialsGrantType())
            {

                var foundObj = await _applicationManager.FindByClientIdAsync(model.ClientId);
                var application = (dynamic)foundObj ??
                                  throw new InvalidOperationException("The application cannot be found.");
                var validation = await _applicationManager.ValidateRedirectUriAsync(foundObj, model.ReturnUrl);
                if (!validation)
                    throw new InvalidOperationException("invalid redirect url");

                var request = new RestRequest(_appSetting["startRequest"], Method.Post);
                var possibleCodes = new List<Color>()
                {
                    Color.blue,
                    Color.green,
                    Color.red,
                    Color.yellow
                };
                var nonce = model.Nonce;
                var index = new Random().Next(0, possibleCodes.Count - 1);
                var startLoginDto = new StartLoginDto()
                {
                    Username = model.Username,
                    ClientId = application.ClientId ?? "IdentityServer",
                    ReturnUrl = model.ReturnUrl,
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
                        ReturnUrl = model.ReturnUrl,
                        Username = model.Username,
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
            }

            return BadRequest(new ApiResponseDto() { errors = "Internal error" });
        }

        [HttpGet("authorize")]
        [HttpPost("authorize")]
        public async Task<IActionResult> Authorize()
        {
            var request = HttpContext.GetOpenIddictServerRequest() ??
                          throw new InvalidOperationException("The OpenID Connect request cannot be retrieved.");

            var result = await HttpContext.AuthenticateAsync();
            if (result == null || !result.Succeeded || request.HasPromptValue(PromptValues.Login) ||
                (request.MaxAge != null && result.Properties?.IssuedUtc != null &&
                 DateTimeOffset.UtcNow - result.Properties.IssuedUtc > TimeSpan.FromSeconds(request.MaxAge.Value)))
            {
                // If the client application requested promptless authentication,
                // return an error indicating that the user is not logged in.
                if (request.HasPromptValue(PromptValues.None))
                {
                    return Forbid(
                        authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                        properties: new AuthenticationProperties(new Dictionary<string, string>
                        {
                            [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.LoginRequired,
                            [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] =
                                "The user is not logged in."
                        }));
                }

                // To avoid endless login -> authorization redirects, the prompt=login flag
                // is removed from the authorization request payload before redirecting the user.
                var prompt = string.Join(" ", request.GetPromptValues().Remove(PromptValues.Login));

                var parameters = Request.HasFormContentType
                    ? Request.Form.Where(parameter => parameter.Key != Parameters.Prompt).ToList()
                    : Request.Query.Where(parameter => parameter.Key != Parameters.Prompt).ToList();

                parameters.Add(KeyValuePair.Create(Parameters.Prompt, new StringValues(prompt)));

                // For scenarios where the default challenge handler configured in the ASP.NET Core
                // authentication options shouldn't be used, a specific scheme can be specified here.
                return Challenge(new AuthenticationProperties
                {
                    RedirectUri = _appSetting["IdentityUrlBase"] + "Account/Login" + QueryString.Create(parameters)
                });
            }

            return BadRequest();
        }


        [HttpPost]
        [Route("check")]
        public async Task<IActionResult> Check(CheckInputModel model, [FromQuery] string client_Id)
        {

            var requestAddict = HttpContext.GetOpenIddictServerRequest();

            var resultAddict = await HttpContext.AuthenticateAsync();

            //var iddictRequest = HttpContext.GetOpenIddictServerRequest();
            //if (iddictRequest.IsClientCredentialsGrantType())
            {

                var request = new RestRequest(_appSetting["checkRequest"] + "?sessionId=" + model.SessionId,
                    Method.Get);
                var result = await _myRestClient.ExecuteAsync(request);

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
                            var publicCertificate = new X509Certificate2(Convert.FromBase64String(cert.PublicCert));
                            if (publicCertificate.NotAfter < DateTime.UtcNow &&
                                publicCertificate.NotBefore > DateTime.UtcNow)
                            {
                                ModelState.AddModelError("Error", "Invalid Certificate");
                            }

                            var requestCurrentSessionReq =
                                new RestRequest(
                                    $"api/Auth/session?sessionId={model.SessionId}&thumbprint={checkResponceDto.PublicCertThumbprint}&username={checkResponceDto.Username}",
                                    Method.Get);
                            var requestCurrentSessionResult =
                                await _myRestClient.ExecuteAsync(requestCurrentSessionReq);
                            if (requestCurrentSessionResult.IsSuccessful)
                            {
                                var session =
                                    JsonConvert.DeserializeObject<SessionMinDto>(requestCurrentSessionResult.Content);

                                // ComputeHash - returns byte array
                                var isValid = CertHelper.VerifyData(model.RandomString, session.SignedHash,
                                    cert.PublicCert);
                                if (isValid)
                                {
                                    await _signInManager.SignInAsync(new IdentityUser(model.Username),
                                        new OAuthChallengeProperties());
                                    var application = await _applicationManager.FindByClientIdAsync(client_Id) ??
                                                      throw new InvalidOperationException(
                                                          "The application cannot be found.");

                                    // Create a new ClaimsIdentity containing the claims that
                                    // will be used to create an id_token, a token or a code.
                                    var identity = new ClaimsIdentity(
                                        TokenValidationParameters.DefaultAuthenticationType, Claims.Name, Claims.Role);

                                    // Use the client_id as the subject identifier.
                                    identity.SetClaim(Claims.Subject,
                                        await _applicationManager.GetClientIdAsync(application));
                                    identity.SetClaim(Claims.Name,
                                        await _applicationManager.GetDisplayNameAsync(application));

                                    identity.SetDestinations(static claim => claim.Type switch
                                    {
                                        // Allow the "name" claim to be stored in both the access and identity tokens
                                        // when the "profile" scope was granted (by calling principal.SetScopes(...)).
                                        Claims.Name when claim.Subject.HasScope(Scopes.Profile)
                                            => [Destinations.AccessToken, Destinations.IdentityToken],

                                        // Otherwise, only store the claim in the access tokens.
                                        _ => [Destinations.AccessToken]
                                    });

                                    var res = SignIn(new ClaimsPrincipal(identity),
                                        OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

                                    if (res.Principal.Identity.IsAuthenticated)
                                        return res;
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
                    return Ok(new { Redirect = false });
                }

                if (result.Content != null && !result.Content.Contains("Waiting for response"))
                {
                    return BadRequest(new ApiResponseDto() { errors = result.Content });
                }
            }

            return Ok(new { Redirect = false });
        }

        [HttpPost]
        [Route("DeleteUser")]
        public async Task<IActionResult> DeleteUser(LoginInputDto model)
        {
            var restRequest = new RestRequest("api/Signup/delete", Method.Post);
            restRequest.AddJsonBody(new
            {
                Email = model.Username
            });
            var response = await _myRestClient.ExecuteAsync(restRequest);
            if (!response.IsSuccessful)
            {
                var errorResult = JsonConvert.DeserializeObject<ApiResponseDto>(response.Content);

                return BadRequest(new ApiResponseDto() { errors = errorResult.errors });
            }

            return Ok(new { Redirect = false });
        }

        [HttpPost]
        [Route("DeleteConfirm")]
        public async Task<IActionResult> DeleteConfirm(SignupCheckDto model)
        {
            var restRequest = new RestRequest("api/Signup/DeleteConfirmation", Method.Post);
            restRequest.AddJsonBody(model);
            var response = await _myRestClient.ExecuteAsync(restRequest);
            if (!response.IsSuccessful)
            {
                var errorResult = JsonConvert.DeserializeObject<ApiResponseDto>(response.Content);

                return BadRequest(new ApiResponseDto() { errors = errorResult.errors });
            }
            return Ok(new { Redirect = false });
        }


    }

    public class UserDto
    {
        public bool IsLoggedIn { get; set; }
        public string Name { get; set; }
    }
}