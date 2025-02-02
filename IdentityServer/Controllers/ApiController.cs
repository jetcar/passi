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
        [Route("login")]
        public async Task<IActionResult> Login(LoginInputDto model)
        {
            var foundObj = await _applicationManager.FindByClientIdAsync(model.ClientId);
            var application = (dynamic)foundObj ?? throw new InvalidOperationException("The application cannot be found.");
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

            return BadRequest(new ApiResponseDto() { errors = "Internal error" });
        }

        [HttpPost]
        [Route("check")]
        public async Task<IActionResult> Check(CheckInputModel model)
        {
            var request = new RestRequest(_appSetting["checkRequest"] + "?sessionId=" + model.SessionId,
                Method.Get);
            var result = await _myRestClient.ExecuteAsync(request);

            if (result.IsSuccessful)
            {
                var application = _applicationManager.FindByRedirectUriAsync(model.ReturnUrl) ??
                                  throw new InvalidOperationException("The application cannot be found.");

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
                        var requestCurrentSessionResult = await _myRestClient.ExecuteAsync(requestCurrentSessionReq);
                        if (requestCurrentSessionResult.IsSuccessful)
                        {
                            var session =
                                JsonConvert.DeserializeObject<SessionMinDto>(requestCurrentSessionResult.Content);

                            // ComputeHash - returns byte array
                            var isValid = CertHelper.VerifyData(model.RandomString, session.SignedHash,
                                cert.PublicCert);
                            if (isValid)
                            {


                                var identity = new ClaimsIdentity(TokenValidationParameters.DefaultAuthenticationType, Claims.Name, Claims.Role);

                                // Use the client_id as the subject identifier.
                                identity.SetClaim("Thumbprint", checkResponceDto.PublicCertThumbprint);
                                identity.SetClaim("sessionId", model.SessionId.ToString());
                                identity.SetClaim("email", model.Username);
                                identity.SetClaim(Claims.Subject, model.Username);

                                identity.SetDestinations(static claim => claim.Type switch
                                {
                                    // Allow the "name" claim to be stored in both the access and identity tokens
                                    // when the "profile" scope was granted (by calling principal.SetScopes(...)).
                                    Claims.Name when claim.Subject.HasScope(Scopes.Profile)
                                        => [Destinations.AccessToken, Destinations.IdentityToken],

                                    // Otherwise, only store the claim in the access tokens.
                                    _ => [Destinations.AccessToken]
                                });

                                var res = SignIn(new ClaimsPrincipal(identity), OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
                                if (res.Principal.Identity.IsAuthenticated)
                                    return Ok(new { Redirect = model.ReturnUrl });
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