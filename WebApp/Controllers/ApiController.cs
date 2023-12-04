using System.Security.Claims;
using ConfigurationManager;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PostSharp.Extensibility;
using WebApp.Models;

namespace WebApp.Controllers
{
    [Profile(AttributeTargetElements = MulticastTargets.Method)]
    [ApiController]
    [Route("api")]
    public class ApiController : ControllerBase
    {

        [HttpGet]
        [Route("UserInfo")]
        public UserInfoModel UserInfo()
        {
            if (HttpContext.User.Identity.IsAuthenticated)
                return UserInfoModel.Create(HttpContext.User.Identity as ClaimsIdentity);
            return null;
        }
        [HttpGet]
        [Route("UserLoggedIn")]
        public bool UserLoggedIn()
        {
            return HttpContext.User.Identity.IsAuthenticated;
        }



    }
}