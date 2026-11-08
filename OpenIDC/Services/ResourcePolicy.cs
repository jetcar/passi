using System;
using System.Collections.Generic;
using ConfigurationManager;

namespace OpenIDC.Services;

public sealed class ResourcePolicy
{
    public const string Finance = "https://finance.passi.cloud/mcp";
    private readonly HashSet<string> _allowed;

    public ResourcePolicy(AppSetting settings = null)
    {
        var configured = settings?["OAuthAllowedResources"] ?? Finance;
        _allowed = new(configured.Split(' ', StringSplitOptions.RemoveEmptyEntries), StringComparer.Ordinal);
        foreach (var resource in _allowed)
            if (!Uri.TryCreate(resource, UriKind.Absolute, out var uri) || uri.Scheme != "https" ||
                !string.IsNullOrEmpty(uri.UserInfo) || resource.Contains('#'))
                throw new InvalidOperationException("OAuthAllowedResources must contain absolute HTTPS URLs without userinfo or fragments.");
    }

    public bool IsAllowed(string resource) => resource == null || _allowed.Contains(resource);

    // An omitted indicator inherits the grant. It can never upgrade an unbound grant or switch audiences.
    public bool Matches(string granted, string requested) =>
        IsAllowed(granted) && IsAllowed(requested) && (requested == null || requested == granted);
}
