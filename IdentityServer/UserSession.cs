using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using ConfigurationManager;
using IdentityModel;
using IdentityServer4.Configuration;
using IdentityServer4.Extensions;
using IdentityServer4.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace IdentityServer;

public class UserSession : IUserSession
{
    /// <summary>
    /// The HTTP context accessor
    /// </summary>
    protected readonly IHttpContextAccessor HttpContextAccessor;

    /// <summary>
    /// The handlers
    /// </summary>
    protected readonly IAuthenticationHandlerProvider Handlers;

    /// <summary>
    /// The options
    /// </summary>
    protected readonly IdentityServerOptions Options;

    /// <summary>
    /// The clock
    /// </summary>
    protected readonly ISystemClock Clock;

    /// <summary>
    /// The logger
    /// </summary>
    protected readonly ILogger Logger;

    /// <summary>
    /// Gets the HTTP context.
    /// </summary>
    /// <value>
    /// The HTTP context.
    /// </value>
    protected HttpContext HttpContext => HttpContextAccessor.HttpContext;

    /// <summary>
    /// Gets the name of the check session cookie.
    /// </summary>
    /// <value>
    /// The name of the check session cookie.
    /// </value>
    protected string CheckSessionCookieName => Options.Authentication.CheckSessionCookieName;

    /// <summary>
    /// Gets the domain of the check session cookie.
    /// </summary>
    /// <value>
    /// The domain of the check session cookie.
    /// </value>
    protected string CheckSessionCookieDomain => Options.Authentication.CheckSessionCookieDomain;

    /// <summary>
    /// Gets the SameSite mode of the check session cookie.
    /// </summary>
    /// <value>
    /// The SameSite mode of the check session cookie.
    /// </value>
    protected SameSiteMode CheckSessionCookieSameSiteMode => Options.Authentication.CheckSessionCookieSameSiteMode;

    /// <summary>
    /// The principal
    /// </summary>
    protected ClaimsPrincipal Principal;

    /// <summary>
    /// The properties
    /// </summary>
    protected AuthenticationProperties Properties;

    private AppSetting _appSetting;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultUserSession"/> class.
    /// </summary>
    /// <param name="httpContextAccessor">The HTTP context accessor.</param>
    /// <param name="handlers">The handlers.</param>
    /// <param name="options">The options.</param>
    /// <param name="clock">The clock.</param>
    /// <param name="logger">The logger.</param>
    public UserSession(
        IHttpContextAccessor httpContextAccessor,
        IAuthenticationHandlerProvider handlers,
        IdentityServerOptions options,
        ISystemClock clock,
        ILogger<IUserSession> logger, AppSetting appSetting)
    {
        HttpContextAccessor = httpContextAccessor;
        Handlers = handlers;
        Options = options;
        Clock = clock;
        Logger = logger;
        _appSetting = appSetting;
    }

    public async Task<string> CreateSessionIdAsync(ClaimsPrincipal principal, AuthenticationProperties properties)
    {
        if (principal == null) throw new ArgumentNullException(nameof(principal));
        if (properties == null) throw new ArgumentNullException(nameof(properties));

        var currentSubjectId = (await GetUserAsync())?.GetSubjectId();
        var newSubjectId = principal.GetSubjectId();

        if (properties.GetSessionId() == null || currentSubjectId != newSubjectId)
        {
            properties.SetSessionId(CryptoRandom.CreateUniqueId(16, CryptoRandom.OutputFormat.Hex));
        }

        var sid = properties.GetSessionId();
        IssueSessionIdCookie(sid);

        Principal = principal;
        Properties = properties;

        return sid;
    }

    public virtual void IssueSessionIdCookie(string sid)
    {
        if (Options.Endpoints.EnableCheckSessionEndpoint)
        {
            if (HttpContext.Request.Cookies[CheckSessionCookieName] != sid)
            {
                HttpContext.Response.Cookies.Append(
                    Options.Authentication.CheckSessionCookieName,
                    sid,
                    CreateSessionIdCookieOptions());
            }
        }
    }

    public virtual CookieOptions CreateSessionIdCookieOptions()
    {
        var secure = HttpContext.Request.IsHttps;
        var path = HttpContext.GetIdentityServerBasePath().CleanUrlPath();

        var options = new CookieOptions
        {
            HttpOnly = false,
            Secure = secure,
            Path = path,
            IsEssential = true,
            Domain = CheckSessionCookieDomain,
            SameSite = CheckSessionCookieSameSiteMode
        };

        return options;
    }

    public async Task<ClaimsPrincipal> GetUserAsync()
    {
        await AuthenticateAsync();

        if (HttpContext.Request.Path.ToString().ToLower().Contains("/connect/authorize") && !HttpContext.Request.Path
                .ToString().ToLower().Contains("/connect/authorize/callback"))
        {
            await HttpContext.SignOutAsync();
            Properties = null;
            Principal = null;
            return null;
        }

        return Principal;
    }

    protected virtual async Task AuthenticateAsync()
    {
        if (HttpContext.Request.Path.ToString().ToLower().Contains("/connect/authorize") && !HttpContext.Request.Path
                .ToString().ToLower().Contains("/connect/authorize/callback"))
            return;
        if (Principal == null || Properties == null)
        {
            var scheme = await GetCookieAuthenticationSchemeAsync(HttpContext);

            var handler = await Handlers.GetHandlerAsync(HttpContext, scheme);
            if (handler == null)
            {
                throw new InvalidOperationException($"No authentication handler is configured to authenticate for the scheme: {scheme}");
            }

            var result = await handler.AuthenticateAsync();
            if (result != null && result.Succeeded)
            {
                Principal = result.Principal;
                Properties = result.Properties;
                await UpdateSessionCookie();
            }
        }
    }

    internal static async Task<string> GetCookieAuthenticationSchemeAsync(HttpContext context)
    {
        var options = context.RequestServices.GetRequiredService<IdentityServerOptions>();
        if (options.Authentication.CookieAuthenticationScheme != null)
        {
            return options.Authentication.CookieAuthenticationScheme;
        }

        var schemes = context.RequestServices.GetRequiredService<IAuthenticationSchemeProvider>();
        var scheme = await schemes.GetDefaultAuthenticateSchemeAsync();
        if (scheme == null)
        {
            throw new InvalidOperationException("No DefaultAuthenticateScheme found or no CookieAuthenticationScheme configured on IdentityServerOptions.");
        }

        return scheme.Name;
    }

    public virtual async Task<string> GetSessionIdAsync()
    {
        await AuthenticateAsync();

        return Properties?.GetSessionId();
    }

    public async Task EnsureSessionIdCookieAsync()
    {
        var sid = await GetSessionIdAsync();
        if (sid != null)
        {
            IssueSessionIdCookie(sid);
        }
        else
        {
            await RemoveSessionIdCookieAsync();
        }

        //if (HttpContext.Request.Path.ToString().ToLower().Contains("/connect/authorize"))
        //    await RemoveSessionIdCookieAsync();
    }

    public Task RemoveSessionIdCookieAsync()
    {
        if (HttpContext.Request.Cookies.ContainsKey(CheckSessionCookieName))
        {
            HttpContext.Response.Cookies.Delete(CheckSessionCookieName);
        }

        return Task.CompletedTask;
    }

    public virtual async Task AddClientIdAsync(string clientId)
    {
        if (clientId == null) throw new ArgumentNullException(nameof(clientId));

        await AuthenticateAsync();
        if (Properties != null)
        {
            var clientIds = Properties.GetClientList();
            if (!clientIds.Contains(clientId))
            {
                Properties.AddClientId(clientId);
                await UpdateSessionCookie();
            }
        }
    }

    private async Task UpdateSessionCookie()
    {
        await AuthenticateAsync();

        if (Principal == null || Properties == null) throw new InvalidOperationException("User is not currently authenticated");

        var scheme = await GetCookieAuthenticationSchemeAsync(HttpContext);
        Properties.ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(Convert.ToDouble(_appSetting["SessionLength"]));
        await HttpContext.SignInAsync(scheme, Principal, Properties);
    }

    public async Task<IEnumerable<string>> GetClientListAsync()
    {
        await AuthenticateAsync();

        if (Properties != null)
        {
            try
            {
                return Properties.GetClientList();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error decoding client list");
                // clear so we don't keep failing
                Properties.RemoveClientList();
                await UpdateSessionCookie();
            }
        }

        return Enumerable.Empty<string>();
    }
}