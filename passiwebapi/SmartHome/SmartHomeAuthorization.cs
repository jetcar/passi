using System;
using System.Linq;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace passi_webapi.SmartHome;

public sealed class HomeOwnerRequirement : IAuthorizationRequirement { }

public sealed class HomeOwnerHandler(SmartHomeService homes) : AuthorizationHandler<HomeOwnerRequirement>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, HomeOwnerRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated != true) return;
        var subject = context.User.FindFirst("sub")?.Value;
        var thumbprint = context.User.FindFirst("Thumbprint")?.Value;
        if (string.IsNullOrEmpty(subject) || string.IsNullOrEmpty(thumbprint)) return;
        var owner = await homes.ResolveOwnerAsync(subject, thumbprint);
        if (!owner.HasValue) return;
        // This claim is created by the server after resolving the current account from the DB.
        var identity = (ClaimsIdentity)context.User.Identity;
        foreach (var claim in identity.FindAll(SmartHomeRegistration.OwnerClaim).ToArray()) identity.RemoveClaim(claim);
        identity.AddClaim(new Claim(SmartHomeRegistration.OwnerClaim, owner.Value.ToString()));
        context.Succeed(requirement);
    }
}

public sealed class HomeAgentAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger,
    UrlEncoder encoder, SmartHomeService homes)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // UseSmartHomeBoundary rejects query credentials before endpoint authorization.
        // This handler only reads the Authorization header and validates it against stored credentials.
        if (!Request.IsHttps)
            return AuthenticateResult.Fail("Secure header authentication required");
        var authorization = Request.Headers.Authorization.ToString();
        if (!authorization.StartsWith("Bearer ", StringComparison.Ordinal) || authorization.Length > 512)
            return AuthenticateResult.NoResult();
        var agent = await homes.AuthenticateAgentAsync(authorization[7..]);
        if (agent == null) return AuthenticateResult.Fail("Invalid agent credential");
        // Kept on the request only; never serialize credentials into responses or connection URLs.
        Context.Items[typeof(AgentIdentity)] = agent;
        var identity = new ClaimsIdentity(new[] {
            new Claim(ClaimTypes.NameIdentifier, agent.HomeId.ToString()),
        }, Scheme.Name);
        return AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name));
    }
}
