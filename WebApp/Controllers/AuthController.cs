using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using System.Threading.Tasks;
using GoogleTracer;

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
            foreach (var key in Request.Cookies.Keys)
            {
                Response.Cookies.Delete(key, new CookieOptions() { Secure = true });
            }
            return Challenge(new AuthenticationProperties { RedirectUri = "/" }, OpenIdConnectDefaults.AuthenticationScheme);

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