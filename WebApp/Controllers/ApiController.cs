using Microsoft.AspNetCore.Mvc;

using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using GoogleTracer;
using WebApp.Models;

namespace WebApp.Controllers
{
    [Profile]
    [ApiController]
    [Route("api")]
    public class ApiController : ControllerBase
    {
        [HttpGet]
        [Route("userinfo")]
        public List<ClaimDto> UserInfo()
        {
            if (HttpContext.User.Identity.IsAuthenticated)
                return (HttpContext.User.Identity as ClaimsIdentity).Claims.Select(x => new ClaimDto() { Type = x.Type, Value = x.Value }).ToList();
            return new List<ClaimDto>();
        }

        [HttpGet]
        [Route("userloggedin")]
        public bool UserLoggedIn()
        {
            return HttpContext.User.Identity.IsAuthenticated;
        }
    }
}