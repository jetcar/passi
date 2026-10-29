using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using ConfigurationManager;
using Microsoft.AspNetCore.Http;
using RedisClient;

namespace OpenIDC.Services
{
    public interface ILoginRateLimiter
    {
        bool TryAcquire(HttpContext context, string username, out TimeSpan retryAfter);
    }

    public class LoginRateLimiter : ILoginRateLimiter
    {
        private const string KeyPrefix = "ratelimit:openidc:login:";
        private readonly IRedisService _redis;
        private readonly int _ipPermitLimit;
        private readonly int _usernamePermitLimit;
        private readonly TimeSpan _window;
        private readonly Func<IPAddress, bool> _isTrustedProxy;

        public LoginRateLimiter(IRedisService redis, AppSetting settings)
            : this(
                redis,
                PositiveSetting(settings["LoginRateLimitPerIp"], 10),
                PositiveSetting(settings["LoginRateLimitPerUsername"], 3),
                TimeSpan.FromSeconds(PositiveSetting(settings["LoginRateLimitWindowSeconds"], 300)),
                new TrustedProxyResolver(settings["TrustedProxyHosts"] ?? "haproxy").Contains)
        {
        }

        internal LoginRateLimiter(
            IRedisService redis,
            int ipPermitLimit,
            int usernamePermitLimit,
            TimeSpan window,
            Func<IPAddress, bool> isTrustedProxy = null)
        {
            _redis = redis ?? throw new ArgumentNullException(nameof(redis));
            if (ipPermitLimit <= 0) throw new ArgumentOutOfRangeException(nameof(ipPermitLimit));
            if (usernamePermitLimit <= 0) throw new ArgumentOutOfRangeException(nameof(usernamePermitLimit));
            if (window <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(window));
            _ipPermitLimit = ipPermitLimit;
            _usernamePermitLimit = usernamePermitLimit;
            _window = window;
            _isTrustedProxy = isTrustedProxy ?? (_ => false);
        }

        public bool TryAcquire(HttpContext context, string username, out TimeSpan retryAfter)
        {
            var ip = ClientIp(context);
            if (!_redis.TryAcquireFixedWindow(Key("ip", ip), _ipPermitLimit, _window, out retryAfter))
                return false;

            var normalizedUsername = (username ?? string.Empty).Trim().Normalize(NormalizationForm.FormKC).ToUpperInvariant();
            return _redis.TryAcquireFixedWindow(
                Key("username", normalizedUsername), _usernamePermitLimit, _window, out retryAfter);
        }

        private string ClientIp(HttpContext context)
        {
            var remoteIp = context?.Connection.RemoteIpAddress;
            if (!_isTrustedProxy(remoteIp))
                return NormalizeIp(remoteIp);

            var forwarded = context.Request.Headers["X-Forwarded-For"].ToString();
            var addresses = forwarded.Split(
                ',',
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            for (var index = addresses.Length - 1; index >= 0; index--)
            {
                if (IPAddress.TryParse(addresses[index], out var forwardedIp) && !_isTrustedProxy(forwardedIp))
                    return NormalizeIp(forwardedIp);
            }

            return NormalizeIp(remoteIp);
        }

        private static string NormalizeIp(IPAddress address)
        {
            if (address == null) return "unknown";
            return address.IsIPv4MappedToIPv6 ? address.MapToIPv4().ToString() : address.ToString();
        }

        private static string Key(string dimension, string value)
        {
            var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
            return $"{KeyPrefix}{dimension}:{hash}";
        }

        private static int PositiveSetting(string value, int fallback) =>
            int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var parsed) && parsed > 0
                ? parsed
                : fallback;

        private sealed class TrustedProxyResolver
        {
            private static readonly TimeSpan RefreshInterval = TimeSpan.FromMinutes(1);
            private readonly string[] _hosts;
            private readonly object _sync = new();
            private HashSet<string> _addresses = new(StringComparer.Ordinal);
            private DateTime _refreshAfter = DateTime.MinValue;

            public TrustedProxyResolver(string hosts) =>
                _hosts = hosts.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            public bool Contains(IPAddress address)
            {
                if (address == null) return false;
                RefreshIfNeeded();
                lock (_sync)
                    return _addresses.Contains(NormalizeIp(address));
            }

            private void RefreshIfNeeded()
            {
                var now = DateTime.UtcNow;
                lock (_sync)
                {
                    if (now < _refreshAfter) return;

                    var refreshed = new HashSet<string>(StringComparer.Ordinal);
                    foreach (var host in _hosts)
                    {
                        try
                        {
                            foreach (var address in Dns.GetHostAddresses(host))
                                refreshed.Add(NormalizeIp(address));
                        }
                        catch (SocketException)
                        {
                        }
                    }

                    _addresses = refreshed;
                    _refreshAfter = now.Add(RefreshInterval);
                }
            }
        }
    }
}
