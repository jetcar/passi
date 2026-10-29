using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using NUnit.Framework;
using OpenIDC.Services;
using RedisClient;

namespace OpenIDCTests
{
    public class LoginRateLimiterTests
    {
        [Test]
        public void SameIpIsLimitedAcrossDifferentUsernames()
        {
            var limiter = new LoginRateLimiter(new InMemoryRedisService(), 2, 10, TimeSpan.FromMinutes(5));

            Assert.That(Acquire(limiter, "192.0.2.10", "one@passi.cloud"), Is.True);
            Assert.That(Acquire(limiter, "192.0.2.10", "two@passi.cloud"), Is.True);
            Assert.That(Acquire(limiter, "192.0.2.10", "three@passi.cloud"), Is.False);
        }

        [Test]
        public void SameUsernameIsLimitedAcrossDifferentIpsAndCasing()
        {
            var limiter = new LoginRateLimiter(new InMemoryRedisService(), 10, 2, TimeSpan.FromMinutes(5));

            Assert.That(Acquire(limiter, "192.0.2.10", " Alice@Passi.Cloud "), Is.True);
            Assert.That(Acquire(limiter, "192.0.2.11", "alice@passi.cloud"), Is.True);
            Assert.That(Acquire(limiter, "192.0.2.12", "ALICE@PASSI.CLOUD"), Is.False);
        }

        [Test]
        public void RedisKeysDoNotExposeUsernameOrIp()
        {
            var redis = new InMemoryRedisService();
            var limiter = new LoginRateLimiter(redis, 10, 10, TimeSpan.FromMinutes(5));

            Acquire(limiter, "192.0.2.10", "alice@passi.cloud");

            Assert.That(redis.Keys, Has.Count.EqualTo(2));
            Assert.That(redis.Keys, Has.None.Contains("192.0.2.10"));
            Assert.That(redis.Keys, Has.None.Contains("alice@passi.cloud"));
        }

        [Test]
        public void IgnoresUnprocessedForwardedHeader()
        {
            var limiter = new LoginRateLimiter(new InMemoryRedisService(), 2, 10, TimeSpan.FromMinutes(5));

            Assert.That(AcquireForwarded(limiter, "203.0.113.1", "one@passi.cloud"), Is.True);
            Assert.That(AcquireForwarded(limiter, "203.0.113.2", "two@passi.cloud"), Is.True);
            Assert.That(AcquireForwarded(limiter, "203.0.113.3", "three@passi.cloud"), Is.False);
        }

        [Test]
        public void UsesLastForwardedAddressOnlyFromTrustedProxy()
        {
            var limiter = new LoginRateLimiter(
                new InMemoryRedisService(),
                2,
                10,
                TimeSpan.FromMinutes(5),
                ip => ip.ToString() == "172.18.0.2");

            Assert.That(AcquireForwarded(limiter, "203.0.113.1, 192.0.2.10", "one@passi.cloud"), Is.True);
            Assert.That(AcquireForwarded(limiter, "203.0.113.2, 192.0.2.10", "two@passi.cloud"), Is.True);
            Assert.That(AcquireForwarded(limiter, "203.0.113.3, 192.0.2.10", "three@passi.cloud"), Is.False);
        }

        [Test]
        public void SkipsTrustedProxyHopsFromRightToLeft()
        {
            var limiter = new LoginRateLimiter(
                new InMemoryRedisService(),
                2,
                10,
                TimeSpan.FromMinutes(5),
                ip => ip.ToString() is "172.18.0.2" or "198.51.100.20");

            Assert.That(AcquireForwarded(limiter, "192.0.2.10, 198.51.100.20", "one@passi.cloud"), Is.True);
            Assert.That(AcquireForwarded(limiter, "192.0.2.10, 198.51.100.20", "two@passi.cloud"), Is.True);
            Assert.That(AcquireForwarded(limiter, "192.0.2.10, 198.51.100.20", "three@passi.cloud"), Is.False);
        }

        private static bool Acquire(LoginRateLimiter limiter, string ip, string username)
        {
            var context = new DefaultHttpContext();
            context.Connection.RemoteIpAddress = System.Net.IPAddress.Parse(ip);
            return limiter.TryAcquire(context, username, out _);
        }

        private static bool AcquireForwarded(LoginRateLimiter limiter, string forwardedFor, string username)
        {
            var context = new DefaultHttpContext();
            context.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("172.18.0.2");
            context.Request.Headers["X-Forwarded-For"] = forwardedFor;
            return limiter.TryAcquire(context, username, out _);
        }

        private class InMemoryRedisService : IRedisService
        {
            private readonly Dictionary<string, int> _counts = new();
            public List<string> Keys { get; } = new();

            public bool TryAcquireFixedWindow(string key, int permitLimit, TimeSpan window, out TimeSpan retryAfter)
            {
                Keys.Add(key);
                var count = _counts.TryGetValue(key, out var current) ? current + 1 : 1;
                _counts[key] = count;
                retryAfter = count > permitLimit ? window : TimeSpan.Zero;
                return count <= permitLimit;
            }

            public void Add<T>(string key, T item, TimeSpan expire) { }
            public void Add<T>(string key, T item) { }
            public T Get<T>(string key) => default;
            public void Delete<T>(string key) { }
            public T GetAndDelete<T>(string key) => default;
        }
    }
}
