using ConfigurationManager;
using GoogleTracer;
using Microsoft.AspNetCore.Mvc;

namespace WebApp.Controllers
{
    [Profile]
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