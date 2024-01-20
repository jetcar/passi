using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using System.Web;
using ConfigurationManager;
using IdentityServer.Controllers.ViewInputs;
using IdentityServer.Controllers.ViewModels;
using IdentityServer4.Events;
using IdentityServer4;
using IdentityServer4.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using passi_webapi.Dto;
using PostSharp.Extensibility;
using RestSharp;
using Services;
using WebApiDto;
using WebApiDto.Auth;
using IdentityServer.services;
using Microsoft.Extensions.Logging;

namespace IdentityServer.Controllers
{
    [Profile(AttributeTargetElements = MulticastTargets.Method)]
    [ApiController]
    [Route("api")]
    public class ApiController : ControllerBase
    {

        private AppSetting _appSetting;
        private IMyRestClient _myRestClient;
        private IRandomGenerator _randomGenerator;
        private readonly IIdentityServerInteractionService _interaction;
        private readonly IEventService _events;
        public ILogger<ApiController> Logger { get; set; }

        public ApiController(IRandomGenerator randomGenerator, IMyRestClient myRestClient, AppSetting appSetting, IIdentityServerInteractionService interaction, IEventService events, ILogger<ApiController> logger)
        {
            _randomGenerator = randomGenerator;
            _myRestClient = myRestClient;
            _appSetting = appSetting;
            _interaction = interaction;
            _events = events;
            Logger = logger;
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
        [Route("check")]
        public async Task<IActionResult> Check(CheckInputModel model)
        {
            var context = await _interaction.GetAuthorizationContextAsync(model.ReturnUrl);

            var request = new RestRequest(_appSetting["checkRequest"] + "?sessionId=" + model.SessionId, Method.Get);
            var result = _myRestClient.ExecuteAsync(request).Result;

            if (result.IsSuccessful)
            {
                var checkResponceDto = JsonConvert.DeserializeObject<CheckResponceDto>(result.Content);
                await _events.RaiseAsync(new UserLoginSuccessEvent(model.Username, model.Username, model.Username,
                    clientId: context?.Client.ClientId));
                var isuser = new IdentityServerUser(model.Username)
                {
                    DisplayName = model.Username,
                };
                if (checkResponceDto.PublicCertThumbprint != null)
                {
                    var request2 =
                        new RestRequest($"api/Certificate/Public?thumbprint={checkResponceDto.PublicCertThumbprint}&username={checkResponceDto.Username}",
                            Method.Get);
                    var result2 = _myRestClient.ExecuteAsync(request2).Result;
                    if (result2.IsSuccessful)
                    {
                        var cert = JsonConvert.DeserializeObject<CertificateDto>(result2.Content);
                        var publicCertificate = new X509Certificate2(Convert.FromBase64String(cert.PublicCert));
                        if (publicCertificate.NotAfter < DateTime.UtcNow && publicCertificate.NotBefore > DateTime.UtcNow)
                        {
                            ModelState.AddModelError("Error", "Invalid Certificate");
                        }
                        var requestCurrentSessionReq = new RestRequest($"api/Auth/session?sessionId={model.SessionId}&thumbprint={checkResponceDto.PublicCertThumbprint}&username={checkResponceDto.Username}", Method.Get);
                        var requestCurrentSessionResult = _myRestClient.ExecuteAsync(requestCurrentSessionReq).Result;
                        if (requestCurrentSessionResult.IsSuccessful)
                        {
                            var session = JsonConvert.DeserializeObject<SessionMinDto>(requestCurrentSessionResult.Content);

                            // ComputeHash - returns byte array
                            var isValid = CertHelper.VerifyData(model.RandomString, session.SignedHash, cert.PublicCert);
                            if (isValid)
                            {
                                isuser.AdditionalClaims.Add(new Claim("Thumbprint", checkResponceDto.PublicCertThumbprint));
                                isuser.AdditionalClaims.Add(new Claim("sessionId", model.SessionId.ToString()));
                                await AuthenticationManagerExtensions.SignInAsync(HttpContext, isuser,
                                    new AuthenticationProperties()
                                    {
                                        IsPersistent = true,
                                        AllowRefresh = true,
                                        ExpiresUtc =
                                            DateTimeOffset.UtcNow.AddMinutes(Convert.ToDouble(_appSetting["SessionLength"]))
                                    });
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
                        return BadRequest(new ApiResponseDto() { errors = result2.Content + " " + result2.ErrorMessage });

                    }
                }
            }

            if (result.Content == null)
            {
                return Ok(new { Redirect = false });
            }

            if (result.Content != null && !result.Content.Contains("Waiting for response"))
            {
                var errorResult = JsonConvert.DeserializeObject<ApiResponseDto<string>>(result.Content);
                return BadRequest(new ApiResponseDto() { errors = errorResult.errors });
            }

            return Ok(new { Redirect = false });
        }
        [HttpPost]
        [Route("login")]
        public async Task<IActionResult> Login(LoginInputDto model)
        {
            ServicePointManager.ServerCertificateValidationCallback +=
                (sender, cert, chain, sslPolicyErrors) => true;

            var context = await _interaction.GetAuthorizationContextAsync(model.ReturnUrl);

            var request = new RestRequest(_appSetting["startRequest"], Method.Post);
            var possibleCodes = new List<Color>()
                {
                    Color.blue,
                    Color.green,
                    Color.red,
                    Color.yellow
                };
            var queryString = HttpUtility.ParseQueryString(model.ReturnUrl);
            var redirect_uri = queryString.Get("redirect_uri");
            var nonce = HttpUtility.ParseQueryString(model.ReturnUrl).Get("nonce");
            var index = new Random().Next(0, possibleCodes.Count - 1);
            var startLoginDto = new StartLoginDto()
            {
                Username = model.Username,
                ClientId = context?.Client.ClientId ?? "IdentityServer",
                ReturnUrl = redirect_uri,
                CheckColor = possibleCodes[index],
                RandomString = nonce ?? _randomGenerator.GetNumbersString(10)
            };
            request.AddJsonBody(startLoginDto);
            var result = _myRestClient.ExecuteAsync(request).Result;
            if (result.IsSuccessful)
            {
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
            Logger.LogDebug(result.Content);
            var errorResult = JsonConvert.DeserializeObject<ApiResponseDto>(result.Content);

            return BadRequest(new ApiResponseDto() { errors = errorResult.errors });
        }



    }

    public class UserDto
    {
        public bool IsLoggedIn { get; set; }
        public string Name { get; set; }
    }
}