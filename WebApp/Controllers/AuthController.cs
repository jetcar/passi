using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using System.Threading.Tasks;
using GoogleTracer;
using OpenIddict.Client.AspNetCore;

namespace WebApp.Controllers
{
    [Profile]
    public class AuthController : Controller
    {
        public AuthController()
        {
        }

        public IActionResult Login(string returnUrl = "/")
        {

            // Ask the OpenIddict client middleware to redirect the user agent to the identity provider.
            foreach (var key in Request.Cookies.Keys)
            {
                Response.Cookies.Delete(key, new CookieOptions() { Secure = true });
            }
            return Challenge(new AuthenticationProperties { RedirectUri = "/" }, OpenIddictClientAspNetCoreDefaults.AuthenticationScheme);

        }

        [Authorize]
        public async Task<RedirectResult> Logout()
        {
            foreach (var key in Request.Cookies.Keys)
            {
                Response.Cookies.Delete(key, new CookieOptions() { Secure = true });
            }

            return Redirect("/");
            //await HttpContext.SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme);
        }
    }
}