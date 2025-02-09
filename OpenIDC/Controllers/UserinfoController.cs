using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using static OpenIddict.Abstractions.OpenIddictConstants;


public class UserinfoController : Controller
{
    private readonly UserManager<IdentityUser> _userManager;

    public UserinfoController(UserManager<IdentityUser> userManager)
        => _userManager = userManager;

    //
    // GET: /api/userinfo
    [Authorize(AuthenticationSchemes = OpenIddictServerAspNetCoreDefaults.AuthenticationScheme)]
    [HttpGet("~/connect/userinfo"), HttpPost("~/connect/userinfo"), Produces("application/json")]
    public Task<IActionResult> Userinfo()
    {
        var user = new IdentityUser(User.GetClaim(Claims.Subject));

        var claims = new Dictionary<string, string>();
        foreach (Claim userClaim in User.Claims)
        {
            claims[userClaim.Type] = userClaim.Value;
        }

        // Note: the complete list of standard claims supported by the OpenID Connect specification
        // can be found here: http://openid.net/specs/openid-connect-core-1_0.html#StandardClaims

        return Task.FromResult<IActionResult>(Ok(claims));
    }
}
