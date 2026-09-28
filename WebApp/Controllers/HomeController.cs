using System;
using GoogleTracer;
using Microsoft.AspNetCore.Mvc;

namespace WebApp.Controllers
{
    [Profile]
    public class HomeController : Controller
    {
        /// <summary>Legacy link target; the old identity-server clients page it pointed to no longer exists.</summary>
        public IActionResult DevTools() => Redirect("/Home/RegisterApp");

        /// <summary>"Register website": the user's OAuth apps page, logging in first if needed.</summary>
        public IActionResult RegisterApp()
        {
            const string oauthApps = "/OAuthApps";
            return User.Identity?.IsAuthenticated == true
                ? Redirect(oauthApps)
                : Redirect($"/Auth/Login?returnUrl={Uri.EscapeDataString(oauthApps)}");
        }
    }
}