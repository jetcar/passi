﻿using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Options;

///
/// Adapted from https://github.com/IdentityServer/IdentityServer4.Samples/tree/master/Clients/src/MvcHybridAutomaticRefresh/AutomaticTokenManagement
///
namespace WebApp.AutomaticTokenManagement
{
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