using System;
using System.Collections.Generic;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using passi_webapi.Filters;
using Repos;

namespace PassiWebApiTests.Filters
{
    public class AuthorizationFilterTests : TestBase
    {
        private static AuthorizationFilterContext CreateFilterContext(IServiceProvider services, ClaimsPrincipal user)
        {
            var httpContext = new DefaultHttpContext { RequestServices = services, User = user };
            var actionContext = new Microsoft.AspNetCore.Mvc.ActionContext(
                httpContext, new RouteData(), new ActionDescriptor());
            return new AuthorizationFilterContext(actionContext, new List<IFilterMetadata>());
        }

        // The request never reached authentication (no identity at all), which is exactly what a
        // bare AuthorizationFilterContext looks like outside the full ASP.NET Core pipeline. The
        // filter must deny this, not silently let it through to the admin-only action.
        [Test]
        public void RequestWithNoIdentityIsDenied()
        {
            using var scope = ServiceProvider.CreateScope();
            var filterContext = CreateFilterContext(scope.ServiceProvider, new ClaimsPrincipal());

            new Authorization().OnAuthorization(filterContext);

            Assert.That(filterContext.Result, Is.InstanceOf<Microsoft.AspNetCore.Mvc.UnauthorizedResult>());
        }

        [Test]
        public void RequestFromNonAdminIdentityIsDenied()
        {
            using var scope = ServiceProvider.CreateScope();
            var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "not-an-admin@passi.cloud") }, "TestAuth");
            var filterContext = CreateFilterContext(scope.ServiceProvider, new ClaimsPrincipal(identity));

            new Authorization().OnAuthorization(filterContext);

            Assert.That(filterContext.Result, Is.InstanceOf<Microsoft.AspNetCore.Mvc.UnauthorizedResult>());
        }

        [Test]
        public void RequestFromAdminIdentityIsAllowed()
        {
            using var scope = ServiceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<PassiDbContext>();
            var email = $"{Guid.NewGuid()}@passi.cloud";
            dbContext.Admins.Add(new Models.AdminDb { Email = email });
            dbContext.SaveChanges();

            var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, email) }, "TestAuth");
            var filterContext = CreateFilterContext(scope.ServiceProvider, new ClaimsPrincipal(identity));

            new Authorization().OnAuthorization(filterContext);

            Assert.That(filterContext.Result, Is.Null);
        }
    }
}
