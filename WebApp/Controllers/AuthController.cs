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
            // Clear cookies and redirect to new custom OIDC login endpoint
            foreach (var key in Request.Cookies.Keys)
            {
                Response.Cookies.Delete(key, new CookieOptions() { Secure = true });
            }
            return Redirect($"/login?returnUrl={returnUrl}");
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