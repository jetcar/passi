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

        public IActionResult Index()
        {
            return View();
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
    }
}