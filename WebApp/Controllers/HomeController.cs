using System;
using System.Diagnostics;
using System.Net.Http;
using System.Security.Claims;
using System.Threading.Tasks;
using ConfigurationManager;
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
    [ApiController]
    [Route("api/[controller]")]
    public class HomeController : Controller
    {
        private AppSetting _appSetting;
        public HomeController(AppSetting appSetting)
        {
            _appSetting = appSetting;
        }

        public IActionResult Index()
        {
            return View();
        }

        [Authorize]
        public UserInfoModel UserInfo()
        {
            return UserInfoModel.Create(HttpContext.User.Identity as ClaimsIdentity);
        }
        [Authorize]
        public bool UserLoggedIn()
        {
            return true;
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