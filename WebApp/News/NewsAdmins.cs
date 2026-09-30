using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using ConfigurationManager;

namespace WebApp.News
{
    /// <summary>Who may write news posts: the emails listed in the NewsAdminEmails setting (comma or semicolon separated).</summary>
    public class NewsAdmins
    {
        private readonly HashSet<string> _emails;

        public NewsAdmins(AppSetting appSetting)
        {
            _emails = (appSetting["NewsAdminEmails"] ?? "")
                .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
        }

        public bool IsAdmin(ClaimsPrincipal user) => EmailOf(user) is { } email && _emails.Contains(email);

        public static string EmailOf(ClaimsPrincipal user)
        {
            if (user?.Identity?.IsAuthenticated != true)
                return null;

            return user.FindFirst(ClaimTypes.Email)?.Value ?? user.FindFirst("email")?.Value;
        }
    }
}
