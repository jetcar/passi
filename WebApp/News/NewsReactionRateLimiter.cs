using System;
using System.Collections.Concurrent;
using System.Linq;
using Microsoft.AspNetCore.Http;

namespace WebApp.News
{
    /// <summary>
    /// Fixed-window limit on anonymous reaction writes per client IP, so a script cannot inflate counts by
    /// cycling visitor cookies. In-memory: fine for the single webapp instance.
    /// </summary>
    public class NewsReactionRateLimiter
    {
        private readonly int _maxPerWindow;
        private readonly TimeSpan _window;
        private readonly Func<DateTime> _utcNow;
        private readonly ConcurrentDictionary<string, Window> _windows = new();

        public NewsReactionRateLimiter() : this(30, TimeSpan.FromMinutes(1)) { }

        public NewsReactionRateLimiter(int maxPerWindow, TimeSpan window, Func<DateTime> utcNow = null)
        {
            _maxPerWindow = maxPerWindow;
            _window = window;
            _utcNow = utcNow ?? (() => DateTime.UtcNow);
        }

        public bool TryAcquire(HttpContext context)
        {
            var now = _utcNow();
            if (_windows.Count > 10_000)
            {
                foreach (var stale in _windows.Where(w => now - w.Value.Start >= _window).Select(w => w.Key).ToList())
                    _windows.TryRemove(stale, out _);
            }

            var window = _windows.AddOrUpdate(
                ClientIp(context),
                _ => new Window(now, 1),
                (_, current) => now - current.Start >= _window ? new Window(now, 1) : current with { Count = current.Count + 1 });
            return window.Count <= _maxPerWindow;
        }

        /// <summary>
        /// HAProxy (option forwardfor) appends the real peer address to X-Forwarded-For, so the last entry is the one
        /// a client cannot spoof.
        /// </summary>
        public static string ClientIp(HttpContext context)
        {
            var forwarded = context.Request.Headers["X-Forwarded-For"].ToString();
            var last = forwarded.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).LastOrDefault();
            return last ?? context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        }

        private record Window(DateTime Start, int Count);
    }
}
