using System.Diagnostics;
using ConfigurationManager;
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
        public HomeController(AppSetting appSetting)
        {
            _appSetting = appSetting;
        }

        public IActionResult DevTools()
        {
            var url = _appSetting["IdentityUrl"] + _appSetting["ClientsPage"];
            return RedirectPermanent(url);
        }
    }
}