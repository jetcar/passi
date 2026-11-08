using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using ConfigurationManager;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using OpenIDC.Models;
using RedisClient;

namespace OpenIDC.Services;

public sealed class DynamicRegistrationRequest
{
    [JsonPropertyName("redirect_uris")] public List<string> RedirectUris { get; set; }
    [JsonPropertyName("client_name")] public string ClientName { get; set; }
    [JsonPropertyName("token_endpoint_auth_method")] public string AuthMethod { get; set; } = "none";
    [JsonPropertyName("grant_types")] public List<string> GrantTypes { get; set; } = new() { "authorization_code", "refresh_token" };
    [JsonPropertyName("response_types")] public List<string> ResponseTypes { get; set; } = new() { "code" };
    [JsonPropertyName("scope")] public string Scope { get; set; } = "openid profile email";
    // Reject confidential settings. Unknown descriptive metadata (including URLs) is discarded, never fetched.
    [JsonPropertyName("client_secret")] public string ClientSecret { get; set; }
    [JsonPropertyName("jwks_uri")] public string JwksUri { get; set; }
    [JsonPropertyName("jwks")] public object Jwks { get; set; }
}

public sealed class DynamicClientRegistration
{
    public const string ClientIdPrefix = "mcp_";
    public const string KeyPrefix = "oidc:dynamic:";
    private readonly IRedisService _redis;
    private readonly int _capacity;
    private readonly int _rateLimit;
    private readonly TimeSpan _lifetime;
    private readonly LoginRateLimiter.TrustedProxyResolver _proxies;

    public DynamicClientRegistration(IRedisService redis, AppSetting settings)
    {
        _redis = redis;
        _capacity = Setting(settings["DynamicClientCapacity"], 10000, 100000);
        _rateLimit = Setting(settings["DynamicClientRegistrationsPerMinute"], 30, 1000);
        _lifetime = TimeSpan.FromDays(Setting(settings["DynamicClientLifetimeDays"], 30, 90));
        _proxies = new(settings["TrustedProxyHosts"] ?? "haproxy");
    }

    public bool TryAcquire(HttpContext context, out TimeSpan retryAfter)
    {
        // Global admission first bounds the number of per-IP keys even during a distributed flood.
        if (!_redis.TryAcquireFixedWindow("ratelimit:openidc:registration", _rateLimit, TimeSpan.FromMinutes(1), out retryAfter))
            return false;
        var ip = LoginRateLimiter.ClientIp(context, _proxies.Contains);
        var key = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(ip)));
        return _redis.TryAcquireFixedWindow("ratelimit:openidc:registration:" + key, 5, TimeSpan.FromMinutes(1), out retryAfter);
    }

    public static string Validate(DynamicRegistrationRequest request)
    {
        if (request == null) return "invalid_client_metadata";
        if (request.RedirectUris == null || request.RedirectUris.Count is < 1 or > 10 ||
            request.RedirectUris.Any(uri => !SafeRedirect(uri))) return "invalid_redirect_uri";
        if (request.AuthMethod != "none" || request.ClientSecret != null || request.JwksUri != null || request.Jwks != null ||
            !ExactSet(request.GrantTypes, "authorization_code", "refresh_token") || !ExactSet(request.ResponseTypes, "code") ||
            request.ClientName?.Length > 200 || request.ClientName?.Any(char.IsControl) == true ||
            request.Scope == null || request.Scope.Length > 200) return "invalid_client_metadata";
        var scopes = request.Scope.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return scopes.Length == 0 || scopes.Any(s => s != "openid" && s != "email" && s != "profile")
            ? "invalid_client_metadata" : null;
    }

    public OidcClient Register(DynamicRegistrationRequest request)
    {
        if (Validate(request) != null) throw new ArgumentException("Invalid client metadata.");
        var now = DateTime.UtcNow;
        var client = new OidcClient
        {
            ClientId = ClientIdPrefix + WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32)),
            DisplayName = request.ClientName ?? "MCP client",
            RedirectUris = request.RedirectUris.ToList(),
            AllowedScopes = request.Scope.Split(' ', StringSplitOptions.RemoveEmptyEntries).Distinct().ToList(),
            GrantTypes = new() { "authorization_code", "refresh_token" },
            IsDynamic = true, IsPublicClient = true, RequiresPkce = true,
            CreatedAt = now, ExpiresAt = now.Add(_lifetime),
        };
        return _redis.TryAddBounded("oidc:dynamic:index", KeyPrefix + client.ClientId, client, _capacity, _lifetime)
            ? client : null;
    }

    private static bool ExactSet(List<string> actual, params string[] expected) =>
        actual != null && actual.Count == expected.Length && expected.All(x => actual.Contains(x, StringComparer.Ordinal));

    internal static bool SafeRedirect(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > 2048 || value.Any(char.IsWhiteSpace) ||
            value.Any(char.IsControl) || value.IndexOfAny(new[] { '*', '#', '\\' }) >= 0 ||
            !Uri.TryCreate(value, UriKind.Absolute, out var uri) || !uri.IsWellFormedOriginalString() ||
            string.IsNullOrEmpty(uri.Host) || !string.IsNullOrEmpty(uri.UserInfo)) return false;
        return uri.Scheme == Uri.UriSchemeHttps ||
            (uri.Scheme == Uri.UriSchemeHttp && (uri.Host == "localhost" || uri.Host == "127.0.0.1" || uri.Host == "[::1]"));
    }

    private static int Setting(string value, int fallback, int maximum) =>
        int.TryParse(value, out var parsed) && parsed > 0 ? Math.Min(parsed, maximum) : fallback;
}
