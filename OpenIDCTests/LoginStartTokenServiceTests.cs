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

            Assert.That(service.TryConsume(ContextWithCookie("different"), token, Request()), Is.False);
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
        }

        private static LoginStartTokenRequest Request(string redirectUri = "https://site/cb") => new()
        {
            ClientId = "SampleApp",
            RedirectUri = redirectUri,
            ResponseType = "code",
            Scope = "openid profile",
            State = "state",
            Nonce = "nonce",
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
