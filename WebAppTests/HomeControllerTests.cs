using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NUnit.Framework;
using WebApp.Controllers;

namespace WebAppTests
{
    public class HomeControllerTests
    {
        private static HomeController Controller(bool authenticated)
        {
            var identity = authenticated
                ? new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "alice") }, "Cookies")
                : new ClaimsIdentity();

            return new HomeController()
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) },
                },
            };
        }

        [Test]
        public void RegisterAppSendsLoggedInUserStraightToOAuthApps()
        {
            var result = Controller(authenticated: true).RegisterApp();

            Assert.That(result, Is.InstanceOf<RedirectResult>());
            var redirect = (RedirectResult)result;
            Assert.That(redirect.Url, Is.EqualTo("/OAuthApps"));
            Assert.That(redirect.Permanent, Is.False);
        }

        [Test]
        public void RegisterAppSendsAnonymousUserToLoginThenBackToOAuthApps()
        {
            var result = Controller(authenticated: false).RegisterApp();

            Assert.That(result, Is.InstanceOf<RedirectResult>());
            var redirect = (RedirectResult)result;
            Assert.That(redirect.Url, Is.EqualTo("/Auth/Login?returnUrl=%2FOAuthApps"));
            Assert.That(redirect.Permanent, Is.False);
        }

        [Test]
        public void LegacyDevToolsLinkNoLongerPointsAtTheRemovedClientsPage()
        {
            // It used to 301 to IdentityUrl + "/client", which no longer exists. Old links/bookmarks land on RegisterApp now,
            // with a temporary redirect so browsers don't cache it again.
            //
            // This uses a literal Redirect rather than RedirectToAction: in production, RedirectToAction(nameof(RegisterApp))
            // threw "System.InvalidOperationException: No route matches the supplied values" at request time (the legacy
            // UseMvc conventional route registered in WebApp/Startup.cs isn't picked up by the endpoint-routing-based link
            // generator that RedirectToActionResult uses), which a unit test against the bare controller can't catch since
            // it never runs real URL generation.
            var result = Controller(authenticated: false).DevTools();

            Assert.That(result, Is.InstanceOf<RedirectResult>());
            var redirect = (RedirectResult)result;
            Assert.That(redirect.Url, Is.EqualTo("/Home/RegisterApp"));
            Assert.That(redirect.Permanent, Is.False);
        }
    }
}
