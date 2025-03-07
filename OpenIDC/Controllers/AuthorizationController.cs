using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using OpenIddict.Client;
using OpenIddict.Server;
using OpenIddict.Server.AspNetCore;
using static OpenIddict.Abstractions.OpenIddictConstants;


public class AuthorizationController : Controller
{
    private readonly IOpenIddictApplicationManager _applicationManager;
    private readonly IOpenIddictAuthorizationManager _authorizationManager;
    private readonly IOpenIddictScopeManager _scopeManager;
    private readonly SignInManager<IdentityUser> _signInManager;
    private readonly UserManager<IdentityUser> _userManager;

    public AuthorizationController(
        IOpenIddictApplicationManager applicationManager,
        IOpenIddictAuthorizationManager authorizationManager,
        IOpenIddictScopeManager scopeManager,
        SignInManager<IdentityUser> signInManager,
        UserManager<IdentityUser> userManager)
    {
        _applicationManager = applicationManager;
        _authorizationManager = authorizationManager;
        _scopeManager = scopeManager;
        _signInManager = signInManager;
        _userManager = userManager;
    }

    // [HttpGet("connect/authorize")]
    // [HttpPost("connect/authorize")]
    public async Task<IActionResult> Authorize2()
    {
        //HttpContext.Features.Get<OpenIddictServerAspNetCoreFeature>().Transaction = new OpenIddictServerTransaction()
        //{
        //    EndpointType = OpenIddictServerEndpointType.Authorization,
        //};
        //var transaction = HttpContext.Features.Get<OpenIddictServerAspNetCoreFeature>()?.Transaction;
        //transaction.Request = new OpenIddictRequest(Request.Query);
        //transaction.EndpointType = OpenIddictServerEndpointType.Authorization;
        //var fullName = typeof(OpenIddictClientEvents.ProcessAuthenticationContext).FullName;
        //var property = transaction.GetProperty<OpenIddictClientEvents.ProcessAuthenticationContext>(fullName);
        var openIddictServerRequest = HttpContext.GetOpenIddictServerRequest();
        var request = openIddictServerRequest ??
                      throw new InvalidOperationException("The OpenID Connect request cannot be retrieved.");

        // Try to retrieve the user principal stored in the authentication cookie and redirect
        // the user agent to the login page (or to an external provider) in the following cases:
        //
        //  - If the user principal can't be extracted or the cookie is too old.
        //  - If prompt=login was specified by the client application.
        //  - If a max_age parameter was provided and the authentication cookie is not considered "fresh" enough.
        //
        // For scenarios where the default authentication handler configured in the ASP.NET Core
        // authentication options shouldn't be used, a specific scheme can be specified here.
        //if (result == null || !result.Succeeded || request.HasPromptValue(PromptValues.Login) ||
        //   (request.MaxAge != null && result.Properties?.IssuedUtc != null &&
        //    DateTimeOffset.UtcNow - result.Properties.IssuedUtc > TimeSpan.FromSeconds(request.MaxAge.Value)))
        //{
        //    // If the client application requested promptless authentication,
        //    // return an error indicating that the user is not logged in.
        //    if (request.HasPromptValue(PromptValues.None))
        //    {
        //        return Forbid(
        //            authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
        //            properties: new AuthenticationProperties(new Dictionary<string, string>
        //            {
        //                [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.LoginRequired,
        //                [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "The user is not logged in."
        //            }));
        //    }

        //    // To avoid endless login -> authorization redirects, the prompt=login flag
        //    // is removed from the authorization request payload before redirecting the user.
        //    var prompt = string.Join(" ", request.GetPromptValues().Remove(PromptValues.Login));

        //    var parameters = Request.HasFormContentType ?
        //        Request.Form.Where(parameter => parameter.Key != Parameters.Prompt).ToList() :
        //        Request.Query.Where(parameter => parameter.Key != Parameters.Prompt).ToList();

        //    parameters.Add(KeyValuePair.Create(Parameters.Prompt, new StringValues(prompt)));

        //    // For scenarios where the default challenge handler configured in the ASP.NET Core
        //    // authentication options shouldn't be used, a specific scheme can be specified here.
        //    return Challenge(new AuthenticationProperties
        //    {
        //        RedirectUri = Request.PathBase + Request.Path + QueryString.Create(parameters)
        //    });
        //}
        IdentityUser identityUser = new IdentityUser("jetcarq@gmail.com");
        await _signInManager.SignInAsync(identityUser, new OAuthChallengeProperties());
        var result = await HttpContext.AuthenticateAsync();

        // Retrieve the profile of the logged in user.
        //var user = await _userManager.GetUserAsync(result.Principal) ??
        //    throw new InvalidOperationException("The user details cannot be retrieved.");

        // Retrieve the application details from the database.
        var application = await _applicationManager.FindByClientIdAsync(request.ClientId) ??
            throw new InvalidOperationException("Details concerning the calling client application cannot be found.");

        // Retrieve the permanent authorizations associated with the user and the calling client application.
        var authorizations = await _authorizationManager.FindAsync(
            subject: identityUser.UserName,
            client: await _applicationManager.GetIdAsync(application),
            status: Statuses.Valid,
            type: AuthorizationTypes.Permanent,
            scopes: request.GetScopes()).ToListAsync();


        // Create the claims-based identity that will be used by OpenIddict to generate tokens.
        var identity = new ClaimsIdentity(
            authenticationType: TokenValidationParameters.DefaultAuthenticationType,
            nameType: Claims.Name,
            roleType: Claims.Role);

        // Add the claims that will be persisted in the tokens.
        identity.SetClaim(Claims.Subject, identityUser.UserName)
                .SetClaim(Claims.Email, identityUser.UserName)
                ;

        // Note: in this sample, the granted scopes match the requested scope
        // but you may want to allow the user to uncheck specific scopes.
        // For that, simply restrict the list of scopes before calling SetScopes.
        identity.SetScopes(request.GetScopes());
        identity.SetResources(await _scopeManager.ListResourcesAsync(identity.GetScopes()).ToListAsync());

        // Automatically create a permanent authorization to avoid requiring explicit consent
        // for future authorization or token requests containing the same scopes.
        var authorization = authorizations.LastOrDefault();
        authorization ??= await _authorizationManager.CreateAsync(
            identity: identity,
            subject: identityUser.UserName,
            client: await _applicationManager.GetIdAsync(application),
            type: AuthorizationTypes.Permanent,
            scopes: identity.GetScopes());

        identity.SetAuthorizationId(await _authorizationManager.GetIdAsync(authorization));

        return SignIn(new ClaimsPrincipal(identity), OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);


    }


    // to redirect the user agent to the client application using the appropriate response_mode.
    public IActionResult Deny() => Forbid(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

    [HttpGet("~/connect/logout")]
    public IActionResult Logout() => View();

    [ActionName(nameof(Logout)), HttpPost("~/connect/logout"), ValidateAntiForgeryToken]
    public async Task<IActionResult> LogoutPost()
    {
        // Ask ASP.NET Core Identity to delete the local and external cookies created
        // when the user agent is redirected from the external identity provider
        // after a successful authentication flow (e.g Google or Facebook).
        await _signInManager.SignOutAsync();

        // Returning a SignOutResult will ask OpenIddict to redirect the user agent
        // to the post_logout_redirect_uri specified by the client application or to
        // the RedirectUri specified in the authentication properties if none was set.
        return SignOut(
            authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
            properties: new AuthenticationProperties
            {
                RedirectUri = "/"
            });
    }

    [HttpPost("~/connect/token"), IgnoreAntiforgeryToken, Produces("application/json")]
    public async Task<IActionResult> Exchange()
    {
        var request = HttpContext.GetOpenIddictServerRequest() ??
            throw new InvalidOperationException("The OpenID Connect request cannot be retrieved.");

        if (request.IsAuthorizationCodeGrantType() || request.IsRefreshTokenGrantType())
        {
            // Retrieve the claims principal stored in the authorization code/refresh token.
            var result = await HttpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

            // Retrieve the user profile corresponding to the authorization code/refresh token.
            var subject = result.Principal.GetClaim(Claims.Subject);
            var user = new IdentityUser(subject);
            if (user is null)
            {
                return Forbid(
                    authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                    properties: new AuthenticationProperties(new Dictionary<string, string>
                    {
                        [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidGrant,
                        [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "The token is no longer valid."
                    }));
            }

            // Ensure the user is still allowed to sign in.
            //if (!await _signInManager.CanSignInAsync(user))
            //{
            //    return Forbid(
            //        authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
            //        properties: new AuthenticationProperties(new Dictionary<string, string>
            //        {
            //            [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidGrant,
            //            [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "The user is no longer allowed to sign in."
            //        }));
            //}

            var identity = new ClaimsIdentity(result.Principal.Claims,
                authenticationType: TokenValidationParameters.DefaultAuthenticationType,
                nameType: Claims.Name,
                roleType: Claims.Role);

            // Override the user claims present in the principal in case they
            // changed since the authorization code/refresh token was issued.
            identity.SetClaim(Claims.Subject, result.Principal.GetClaim(Claims.Subject))
                    .SetClaim(Claims.Email, result.Principal.GetClaim(Claims.Email))
                    .SetClaim(Claims.Name, result.Principal.GetClaim(Claims.Name))
                    .SetClaim(Claims.PreferredUsername, result.Principal.GetClaim(Claims.PreferredUsername))
                    .SetClaim("Thumbprint", result.Principal.GetClaim("Thumbprint"))
                    .SetClaim("sessionId", result.Principal.GetClaim("sessionId"))
                    .SetClaims(Claims.Role, [.. (await _userManager.GetRolesAsync(user))])
                    ;

            identity.SetDestinations(GetDestinations);

            // Returning a SignInResult will ask OpenIddict to issue the appropriate access/identity tokens.
            return SignIn(new ClaimsPrincipal(identity), OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        throw new InvalidOperationException("The specified grant type is not supported.");
    }

    private static IEnumerable<string> GetDestinations(Claim claim)
    {
        // Note: by default, claims are NOT automatically included in the access and identity tokens.
        // To allow OpenIddict to serialize them, you must attach them a destination, that specifies
        // whether they should be included in access tokens, in identity tokens or in both.

        switch (claim.Type)
        {
            case Claims.Name or Claims.PreferredUsername:
                yield return Destinations.AccessToken;

                if (claim.Subject.HasScope(Scopes.Profile))
                    yield return Destinations.IdentityToken;

                yield break;

            case Claims.Email:
                yield return Destinations.AccessToken;

                if (claim.Subject.HasScope(Scopes.Email))
                    yield return Destinations.IdentityToken;

                yield break;

            case Claims.Role:
                yield return Destinations.AccessToken;

                if (claim.Subject.HasScope(Scopes.Roles))
                    yield return Destinations.IdentityToken;

                yield break;

            // Never include the security stamp in the access and identity tokens, as it's a secret value.
            case "AspNet.Identity.SecurityStamp": yield break;

            default:
                yield return Destinations.AccessToken;
                yield break;
        }
    }
}
