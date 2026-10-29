using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using NUnit.Framework;
using OpenIDC.Services;
using RedisClient;

namespace OpenIDCTests
{
    public class LoginStartTokenServiceTests
    {
        [Test]
        public void IssuedTokenIsSingleUseAndBoundToBrowserAndAuthorizationRequest()
        {
            var redis = new InMemoryRedisService();
            var service = new LoginStartTokenService(redis, TimeSpan.FromMinutes(5));
            var issueContext = new DefaultHttpContext();
            issueContext.Request.Scheme = "https";
            var request = Request();

            var token = service.Issue(issueContext, request);
            var cookie = CookieValue(issueContext.Response.Headers.SetCookie.ToString());
            var consumeContext = ContextWithCookie(cookie);

            Assert.That(service.TryConsume(consumeContext, token, request), Is.True);
            Assert.That(service.TryConsume(consumeContext, token, request), Is.False);
            Assert.That(redis.LastExpiry, Is.EqualTo(TimeSpan.FromMinutes(5)));
            Assert.That(issueContext.Response.Headers.SetCookie.ToString(), Does.Contain("httponly"));
            Assert.That(issueContext.Response.Headers.SetCookie.ToString(), Does.Contain("secure"));
            Assert.That(issueContext.Response.Headers.SetCookie.ToString(), Does.Contain("samesite=lax"));
        }

        [Test]
        public void TokenCannotBeUsedFromAnotherBrowserSession()
        {
            var service = new LoginStartTokenService(new InMemoryRedisService(), TimeSpan.FromMinutes(5));
            var issueContext = new DefaultHttpContext();
            var token = service.Issue(issueContext, Request());
            var cookie = CookieValue(issueContext.Response.Headers.SetCookie.ToString());

            Assert.That(service.TryConsume(ContextWithCookie("different"), token, Request()), Is.False);
            Assert.That(service.TryConsume(ContextWithCookie(cookie), token, Request()), Is.True);
        }

        [Test]
        public void TokenCannotBeUsedForModifiedAuthorizationRequest()
        {
            var service = new LoginStartTokenService(new InMemoryRedisService(), TimeSpan.FromMinutes(5));
            var issueContext = new DefaultHttpContext();
            var token = service.Issue(issueContext, Request());
            var cookie = CookieValue(issueContext.Response.Headers.SetCookie.ToString());
            var modified = Request("https://attacker.example/cb");

            Assert.That(service.TryConsume(ContextWithCookie(cookie), token, modified), Is.False);
            Assert.That(service.TryConsume(ContextWithCookie(cookie), token, Request()), Is.True);
        }

        [Test]
        public void MissingAndEmptyAuthorizationValuesHaveDifferentBindings()
        {
            var service = new LoginStartTokenService(new InMemoryRedisService(), TimeSpan.FromMinutes(5));
            var issueContext = new DefaultHttpContext();
            var request = Request(loginHint: null);
            var token = service.Issue(issueContext, request);
            var cookie = CookieValue(issueContext.Response.Headers.SetCookie.ToString());
            var withEmptyHint = Request(loginHint: string.Empty);

            Assert.That(service.TryConsume(ContextWithCookie(cookie), token, withEmptyHint), Is.False);
            Assert.That(service.TryConsume(ContextWithCookie(cookie), token, request), Is.True);
        }

        [Test]
        public void IssuingTokenRefreshesExistingBrowserCookieLifetime()
        {
            var service = new LoginStartTokenService(new InMemoryRedisService(), TimeSpan.FromMinutes(5));
            var context = ContextWithCookie("existing-session");

            service.Issue(context, Request());

            Assert.That(context.Response.Headers.SetCookie.ToString(), Does.Contain("existing-session"));
            Assert.That(context.Response.Headers.SetCookie.ToString(), Does.Contain("max-age=3600"));
        }

        [Test]
        public void LoginSessionBindingIsBrowserAndRequestBoundAndSingleUse()
        {
            var redis = new InMemoryRedisService();
            var service = new LoginStartTokenService(
                redis,
                TimeSpan.FromMinutes(5),
                TimeSpan.FromMinutes(10));
            var context = ContextWithCookie("browser-session");
            var request = Request();

            service.BindSession(context, "session-id", request);

            Assert.That(service.IsSessionBound(context, "session-id", request), Is.True);
            Assert.That(service.IsSessionBound(ContextWithCookie("other-browser"), "session-id", request), Is.False);
            Assert.That(service.IsSessionBound(context, "session-id", Request("https://other/cb")), Is.False);
            Assert.That(service.TryConsumeSession(ContextWithCookie("other-browser"), "session-id", request), Is.False);
            Assert.That(service.TryConsumeSession(context, "session-id", Request("https://other/cb")), Is.False);
            Assert.That(service.TryConsumeSession(context, "session-id", request), Is.True);
            Assert.That(service.TryConsumeSession(context, "session-id", request), Is.False);
            Assert.That(redis.LastExpiry, Is.EqualTo(TimeSpan.FromMinutes(10)));
        }

        private static LoginStartTokenRequest Request(
            string redirectUri = "https://site/cb",
            string loginHint = "alice@passi.cloud") => new()
        {
            ClientId = "SampleApp",
            RedirectUri = redirectUri,
            ResponseType = "code",
            Scope = "openid profile",
            State = "state",
            Nonce = "nonce",
            LoginHint = loginHint,
            CodeChallenge = "challenge",
            CodeChallengeMethod = "S256",
        };

        private static DefaultHttpContext ContextWithCookie(string value)
        {
            var context = new DefaultHttpContext();
            context.Request.Headers.Cookie = $"{LoginStartTokenService.BrowserCookieName}={value}";
            return context;
        }

        private static string CookieValue(string setCookie) =>
            setCookie.Split(';', 2)[0].Split('=', 2)[1];

        private sealed class InMemoryRedisService : IRedisService
        {
            private readonly Dictionary<string, object> _values = new();
            public TimeSpan LastExpiry { get; private set; }

            public void Add<T>(string key, T item, TimeSpan expire)
            {
                _values[TypedKey<T>(key)] = item;
                LastExpiry = expire;
            }

            public void Add<T>(string key, T item) => Add(key, item, TimeSpan.FromMinutes(5));
            public T Get<T>(string key) => _values.TryGetValue(TypedKey<T>(key), out var value) ? (T)value : default;
            public void Delete<T>(string key) => _values.Remove(TypedKey<T>(key));

            public T GetAndDelete<T>(string key)
            {
                var typedKey = TypedKey<T>(key);
                if (!_values.Remove(typedKey, out var value)) return default;
                return (T)value;
            }

            private static string TypedKey<T>(string key) => typeof(T).FullName + "." + key;
        }
    }
}
