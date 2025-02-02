using AspNet.Security.OpenId.Steam;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Server.AspNetCore;
using static OpenIddict.Abstractions.OpenIddictConstants;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using OpenIddict.Abstractions;

namespace IdentityServer.Controllers;

[Route("authorize")]
[ApiController]
public class AuthorizationController : ControllerBase
{
    [HttpGet()]
    public async Task<IResult> Index()
    {
        // Resolve the claims stored in the principal created after the Steam authentication dance.
        // If the principal cannot be found, trigger a new challenge to redirect the user to Steam.
        var principal = (await Request.HttpContext.AuthenticateAsync(SteamAuthenticationDefaults.AuthenticationScheme))?.Principal;
        if (principal is null)
        {
            return Results.Challenge(properties: null, [SteamAuthenticationDefaults.AuthenticationScheme]);
        }

        var identifier = principal.FindFirst(ClaimTypes.NameIdentifier)!.Value;

        // Create a new identity and import a few select claims from the Steam principal.
        var identity = new ClaimsIdentity(TokenValidationParameters.DefaultAuthenticationType);
        identity.AddClaim(new Claim(Claims.Subject, identifier));
        identity.AddClaim(new Claim(Claims.Name, identifier).SetDestinations(Destinations.AccessToken));

        return Results.SignIn(new ClaimsPrincipal(identity), properties: null, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

    }
}