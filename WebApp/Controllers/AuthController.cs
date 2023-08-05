using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using AppCommon;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Http;
using Google.Cloud.Diagnostics.Common;
using PostSharp.Extensibility;

namespace WebApp.Controllers
{
    [Profile(AttributeTargetElements = MulticastTargets.Method)]
    public class AuthController : Controller
    {
        private readonly IManagedTracer _tracer;
        public AuthController(IManagedTracer tracer)
        {
            _tracer = tracer;
        }

        public async Task Login(string returnUrl = "/")
        {
            using (_tracer.StartSpan("login"))
            {
                foreach (var key in Request.Cookies.Keys)
                {
                    Response.Cookies.Delete(key, new CookieOptions() { Secure = true });
                }

                await HttpContext.ChallengeAsync(OpenIdConnectDefaults.AuthenticationScheme,
                    new AuthenticationProperties
                    {
                        RedirectUri = returnUrl,
                    });
            }
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