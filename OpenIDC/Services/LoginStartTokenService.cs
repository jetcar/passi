using System;
using System.Security.Cryptography;
using System.Text;
using ConfigurationManager;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using RedisClient;

namespace OpenIDC.Services
{
    public interface ILoginStartTokenService
    {
        string Issue(HttpContext context, LoginStartTokenRequest request);
        bool TryConsume(HttpContext context, string token, LoginStartTokenRequest request);
    }

    public sealed class LoginStartTokenRequest
    {
        public string ClientId { get; init; }
        public string RedirectUri { get; init; }
        public string ResponseType { get; init; }
        public string Scope { get; init; }
        public string State { get; init; }
        public string Nonce { get; init; }
        public string CodeChallenge { get; init; }
        public string CodeChallengeMethod { get; init; }
    }

    public sealed class LoginStartTokenRecord
    {
        public string BrowserSessionHash { get; init; }
        public string AuthorizationRequestHash { get; init; }
    }

    public sealed class LoginStartTokenService : ILoginStartTokenService
    {
        internal const string BrowserCookieName = "Passi-Login-Session";
        private const string KeyPrefix = "openidc:login-start:";
        private static readonly TimeSpan DefaultLifetime = TimeSpan.FromMinutes(5);
        private static readonly TimeSpan BrowserCookieLifetime = TimeSpan.FromHours(1);

        private readonly IRedisService _redis;
        private readonly TimeSpan _lifetime;

        public LoginStartTokenService(IRedisService redis, AppSetting settings)
            : this(redis, Lifetime(settings["LoginStartTokenLifetimeSeconds"]))
        {
        }

        internal LoginStartTokenService(IRedisService redis, TimeSpan lifetime)
        {
            _redis = redis ?? throw new ArgumentNullException(nameof(redis));
            if (lifetime <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(lifetime));
            _lifetime = lifetime;
        }

        public string Issue(HttpContext context, LoginStartTokenRequest request)
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(request);

            var browserSession = context.Request.Cookies[BrowserCookieName];
            if (string.IsNullOrEmpty(browserSession))
            {
                browserSession = RandomToken();
                context.Response.Cookies.Append(BrowserCookieName, browserSession, new CookieOptions
                {
                    HttpOnly = true,
                    IsEssential = true,
                    Secure = context.Request.IsHttps,
                    SameSite = SameSiteMode.Lax,
                    Path = "/",
                    MaxAge = BrowserCookieLifetime,
                });
            }

            var token = RandomToken();
            _redis.Add(Key(token), new LoginStartTokenRecord
            {
                BrowserSessionHash = Hash(browserSession),
                AuthorizationRequestHash = RequestHash(request),
            }, _lifetime);
            return token;
        }

        public bool TryConsume(HttpContext context, string token, LoginStartTokenRequest request)
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(request);

            var browserSession = context.Request.Cookies[BrowserCookieName];
            if (string.IsNullOrWhiteSpace(token) || string.IsNullOrEmpty(browserSession))
                return false;

            var record = _redis.GetAndDelete<LoginStartTokenRecord>(Key(token));
            return record != null &&
                   FixedTimeEquals(record.BrowserSessionHash, Hash(browserSession)) &&
                   FixedTimeEquals(record.AuthorizationRequestHash, RequestHash(request));
        }

        private static string Key(string token) => KeyPrefix + Hash(token);

        private static string RequestHash(LoginStartTokenRequest request)
        {
            var canonical = new StringBuilder();
            Append(canonical, request.ClientId);
            Append(canonical, request.RedirectUri);
            Append(canonical, request.ResponseType);
            Append(canonical, request.Scope);
            Append(canonical, request.State);
            Append(canonical, request.Nonce);
            Append(canonical, request.CodeChallenge);
            Append(canonical, request.CodeChallengeMethod);
            return Hash(canonical.ToString());
        }

        private static void Append(StringBuilder builder, string value)
        {
            value ??= string.Empty;
            builder.Append(value.Length).Append(':').Append(value);
        }

        private static string RandomToken() =>
            WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));

        private static string Hash(string value) =>
            Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

        private static bool FixedTimeEquals(string left, string right)
        {
            if (left == null || right == null || left.Length != right.Length)
                return false;
            return CryptographicOperations.FixedTimeEquals(
                Encoding.ASCII.GetBytes(left),
                Encoding.ASCII.GetBytes(right));
        }

        private static TimeSpan Lifetime(string configuredSeconds) =>
            int.TryParse(configuredSeconds, out var seconds) && seconds > 0
                ? TimeSpan.FromSeconds(seconds)
                : DefaultLifetime;
    }
}
