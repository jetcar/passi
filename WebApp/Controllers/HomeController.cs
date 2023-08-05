using System;
using System.Diagnostics;
using System.Net.Http;
using System.Security.Claims;
using System.Threading.Tasks;
using AppCommon;
using ConfigurationManager;
using Google.Cloud.Diagnostics.Common;
using IdentityModel.Client;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using PostSharp.Extensibility;
using WebApp.Models;

namespace WebApp.Controllers
{
    [Profile(AttributeTargetElements = MulticastTargets.Method)]
    public class HomeController : Controller
    {
        private AppSetting _appSetting;
        private readonly IManagedTracer _tracer;
        public HomeController(AppSetting appSetting, IManagedTracer tracer)
        {
            _appSetting = appSetting;
            _tracer = tracer;
        }

        public IActionResult Index()
        {
            using (_tracer.StartSpan("index"))
                return View();
        }

        [Authorize]
        public IActionResult UserInfo()
        {
            using (_tracer.StartSpan("userInfo"))
                return View(UserInfoModel.Create(HttpContext.User.Identity as ClaimsIdentity));
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            var exceptionHandlerPathFeature = HttpContext.Features.Get<IExceptionHandlerPathFeature>();

            return View(new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
                ErrorMessage = exceptionHandlerPathFeature?.Error.Message,
                RequestPath = exceptionHandlerPathFeature?.Path
            });
        }

        private async Task<string> CallThirdPartyServiceAsync(string url)
        {
            var accessToken = await HttpContext.GetTokenAsync("access_token");

            using (var httpClientHandler = new HttpClientHandler())
            {
                //hack to get around self-signed cert errors in dev
                httpClientHandler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;

                using (var httpClient = new HttpClient(httpClientHandler))
                {
                    httpClient.SetBearerToken(accessToken);
                    try
                    {
                        var content = await httpClient.GetStringAsync(url);
                        return content;
                    }
                    catch (Exception e)
                    {
                        return "tilt: " + e;
                    }
                }
            }
        }

        public IActionResult DevTools()
        {
            var url = _appSetting["IdentityUrl"] + _appSetting["ClientsPage"];
            return RedirectPermanent(url);
        }

        public IActionResult Contacts()
        {
            return View();
        }

        public IActionResult PrivacyPolicy()
        {
            return View();
        }
    }
}