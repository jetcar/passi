using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Repos;

namespace passi_webapi.Filters
{
    public class Authorization : AuthorizeAttribute, IAuthorizationFilter
    {
        public Authorization()
        {
        }

        public void OnAuthorization(AuthorizationFilterContext filterContext)
        {
            var db = filterContext.HttpContext.RequestServices.GetService(typeof(PassiDbContext)) as PassiDbContext;
            var user = filterContext.HttpContext.User.Identity as ClaimsIdentity;
            var userId = user?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (user != null && !db.Admins.Any(x => x.Email == userId))
            {
                filterContext.Result = new UnauthorizedResult();
            }
        }
    }
}