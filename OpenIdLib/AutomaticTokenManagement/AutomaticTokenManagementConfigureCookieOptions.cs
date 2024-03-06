using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Options;
using PostSharp.Extensibility;
using Repos;

namespace OpenIdLib.AutomaticTokenManagement
{
    [Profile(AttributeTargetElements = MulticastTargets.Method)]
    public class AutomaticTokenManagementConfigureCookieOptions : IConfigureNamedOptions<CookieAuthenticationOptions>
    {
        private readonly AuthenticationScheme _scheme;

        public AutomaticTokenManagementConfigureCookieOptions(IAuthenticationSchemeProvider provider)
        {
            _scheme = provider.GetDefaultSignInSchemeAsync().GetAwaiter().GetResult();
        }

        public void Configure(CookieAuthenticationOptions options)
        { }

        public void Configure(string name, CookieAuthenticationOptions options)
        {
            if (name == "Cookies")
            {
                options.EventsType = typeof(AutomaticTokenManagementCookieEvents);
            }
        }
    }
}