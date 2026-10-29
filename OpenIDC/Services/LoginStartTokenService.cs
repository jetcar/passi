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
        void BindSession(HttpContext context, string sessionId, LoginStartTokenRequest request);
        bool IsSessionBound(HttpContext context, string sessionId, LoginStartTokenRequest request);
        bool TryConsumeSession(HttpContext context, string sessionId, LoginStartTokenRequest request);
    }

    public sealed class LoginStartTokenRequest
    {
        public string ClientId { get; init; }
        public string RedirectUri { get; init; }
        public string ResponseType { get; init; }
        public string Scope { get; init; }
        public string State { get; init; }
        public string Nonce { get; init; }
        public string LoginHint { get; init; }
        public string CodeChallenge { get; init; }
        public string CodeChallengeMethod { get; init; }
    }

    public sealed class LoginStartTokenRecord
    {
        public string BrowserSessionHash { get; init; }
        public string AuthorizationRequestHash { get; init; }
    }

    public sealed class LoginSessionBindingRecord
    {
        public string BrowserSessionHash { get; init; }
        public string AuthorizationRequestHash { get; init; }
    }

    public sealed class LoginStartTokenService : ILoginStartTokenService
    {
        internal const string BrowserCookieName = "Passi-Login-Session";
        private const string KeyPrefix = "openidc:login-start:";
        private const string SessionKeyPrefix = "openidc:login-session:";
        private static readonly TimeSpan DefaultLifetime = TimeSpan.FromMinutes(5);
        private static readonly TimeSpan DefaultSessionLifetime = TimeSpan.FromMinutes(10);
        private static readonly TimeSpan BrowserCookieLifetime = TimeSpan.FromHours(1);

        private readonly IRedisService _redis;
        private readonly TimeSpan _lifetime;
        private readonly TimeSpan _sessionLifetime;

        public LoginStartTokenService(IRedisService redis, AppSetting settings)
            : this(
                redis,
                Lifetime(settings["LoginStartTokenLifetimeSeconds"], DefaultLifetime),
                Lifetime(settings["LoginSessionBindingLifetimeSeconds"], DefaultSessionLifetime))
        {
        }

        internal LoginStartTokenService(
            IRedisService redis,
            TimeSpan lifetime,
            TimeSpan? sessionLifetime = null)
        {
            _redis = redis ?? throw new ArgumentNullException(nameof(redis));
            if (lifetime <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(lifetime));
            if (sessionLifetime <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(sessionLifetime));
            _lifetime = lifetime;
            _sessionLifetime = sessionLifetime ?? DefaultSessionLifetime;
        }

        public string Issue(HttpContext context, LoginStartTokenRequest request)
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(request);

            var browserSession = context.Request.Cookies[BrowserCookieName];
            if (string.IsNullOrEmpty(browserSession))
                browserSession = RandomToken();

            context.Response.Cookies.Append(BrowserCookieName, browserSession, new CookieOptions
            {
                HttpOnly = true,
                IsEssential = true,
                Secure = true,
                SameSite = SameSiteMode.Lax,
                Path = "/",
                MaxAge = BrowserCookieLifetime,
            });

            var token = RandomToken();
            var browserSessionHash = Hash(browserSession);
            var authorizationRequestHash = RequestHash(request);
            _redis.Add(Key(token, browserSessionHash, authorizationRequestHash), new LoginStartTokenRecord
            {
                BrowserSessionHash = browserSessionHash,
                AuthorizationRequestHash = authorizationRequestHash,
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

            var browserSessionHash = Hash(browserSession);
            var authorizationRequestHash = RequestHash(request);
            var record = _redis.GetAndDelete<LoginStartTokenRecord>(
                Key(token, browserSessionHash, authorizationRequestHash));
            return record != null &&
                   FixedTimeEquals(record.BrowserSessionHash, browserSessionHash) &&
                   FixedTimeEquals(record.AuthorizationRequestHash, authorizationRequestHash);
        }

        public void BindSession(HttpContext context, string sessionId, LoginStartTokenRequest request)
        {
            var binding = Binding(context, sessionId, request);
            if (binding == null)
                throw new InvalidOperationException("A browser session and login session are required.");

            _redis.Add(binding.Value.Key, new LoginSessionBindingRecord
            {
                BrowserSessionHash = binding.Value.BrowserSessionHash,
                AuthorizationRequestHash = binding.Value.AuthorizationRequestHash,
            }, _sessionLifetime);
        }

        public bool IsSessionBound(HttpContext context, string sessionId, LoginStartTokenRequest request)
        {
            var binding = Binding(context, sessionId, request);
            if (binding == null) return false;
            var record = _redis.Get<LoginSessionBindingRecord>(binding.Value.Key);
            return Matches(record, binding.Value.BrowserSessionHash, binding.Value.AuthorizationRequestHash);
        }

        public bool TryConsumeSession(HttpContext context, string sessionId, LoginStartTokenRequest request)
        {
            var binding = Binding(context, sessionId, request);
            if (binding == null) return false;
            var record = _redis.GetAndDelete<LoginSessionBindingRecord>(binding.Value.Key);
            return Matches(record, binding.Value.BrowserSessionHash, binding.Value.AuthorizationRequestHash);
        }

        private static string Key(string token, string browserSessionHash, string authorizationRequestHash) =>
            KeyPrefix + Hash(token + browserSessionHash + authorizationRequestHash);

        private static (string Key, string BrowserSessionHash, string AuthorizationRequestHash)? Binding(
            HttpContext context,
            string sessionId,
            LoginStartTokenRequest request)
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(request);
            var browserSession = context.Request.Cookies[BrowserCookieName];
            if (string.IsNullOrEmpty(browserSession) || string.IsNullOrWhiteSpace(sessionId)) return null;
            var browserSessionHash = Hash(browserSession);
            var authorizationRequestHash = RequestHash(request);
            return (
                SessionKeyPrefix + Hash(sessionId + browserSessionHash + authorizationRequestHash),
                browserSessionHash,
                authorizationRequestHash);
        }

        private static bool Matches(
            LoginSessionBindingRecord record,
            string browserSessionHash,
            string authorizationRequestHash) =>
            record != null &&
            FixedTimeEquals(record.BrowserSessionHash, browserSessionHash) &&
            FixedTimeEquals(record.AuthorizationRequestHash, authorizationRequestHash);

        private static string RequestHash(LoginStartTokenRequest request)
        {
            var canonical = new StringBuilder();
            Append(canonical, request.ClientId);
            Append(canonical, request.RedirectUri);
            Append(canonical, request.ResponseType);
            Append(canonical, request.Scope);
            Append(canonical, request.State);
            Append(canonical, request.Nonce);
            Append(canonical, request.LoginHint);
            Append(canonical, request.CodeChallenge);
            Append(canonical, request.CodeChallengeMethod);
            return Hash(canonical.ToString());
        }

        private static void Append(StringBuilder builder, string value)
        {
            if (value == null)
            {
                builder.Append("-1:");
                return;
            }
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

        private static TimeSpan Lifetime(string configuredSeconds, TimeSpan fallback) =>
            int.TryParse(configuredSeconds, out var seconds) && seconds > 0
                ? TimeSpan.FromSeconds(seconds)
                : fallback;
    }
}
