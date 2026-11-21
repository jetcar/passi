using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using ConfigurationManager;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace passi_webapi.SmartHome;

// HAProxy terminates TLS, then connects to the API over the private Docker network.
// Trust only exact proxy addresses, never the whole network or a caller-supplied header.
// https://learn.microsoft.com/aspnet/core/host-and-deploy/proxy-load-balancer?view=aspnetcore-10.0
public sealed class SmartHomeForwardingMiddleware
{
    private readonly RequestDelegate next;
    private readonly ILoggerFactory loggerFactory;
    private readonly string hostname;
    private readonly bool hasExplicitAddresses;
    private readonly SemaphoreSlim refresh = new(1, 1);
    private sealed record ForwardingCache(ForwardedHeadersMiddleware Middleware, DateTimeOffset RefreshAt);
    private ForwardingCache cache = new(null, DateTimeOffset.MinValue);

    public SmartHomeForwardingMiddleware(RequestDelegate next, AppSetting settings, ILoggerFactory loggerFactory)
    {
        this.next = next;
        this.loggerFactory = loggerFactory;
        var configured = settings["SmartHomeTrustedProxyAddresses"];
        hasExplicitAddresses = configured != null;
        hostname = settings["SmartHomeTrustedProxyHost"] ?? "haproxy";
        if (hasExplicitAddresses)
        {
            var values = configured.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            // A malformed explicit allowlist disables forwarding; do not fall back to DNS or trust all.
            if (values.Length > 0 && values.All(x => IPAddress.TryParse(x, out _)))
                cache = new(CreateForwarding(values.Select(IPAddress.Parse).ToArray()), DateTimeOffset.MaxValue);
        }
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Path.StartsWithSegments("/api/smarthome") ||
            !context.Request.Headers.ContainsKey("X-Forwarded-Proto"))
        {
            await next(context);
            return;
        }
        if (!hasExplicitAddresses && DateTimeOffset.UtcNow >= Volatile.Read(ref cache).RefreshAt)
        {
            await refresh.WaitAsync(context.RequestAborted);
            try
            {
                if (DateTimeOffset.UtcNow >= cache.RefreshAt)
                {
                    IPAddress[] addresses;
                    using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(2));
                    try
                    {
                        // DNS treats an empty hostname as the local machine; never grant that trust.
                        addresses = string.IsNullOrWhiteSpace(hostname) ? Array.Empty<IPAddress>() :
                            await Dns.GetHostAddressesAsync(hostname, timeout.Token);
                    }
                    catch (Exception error) when (error is SocketException or OperationCanceledException or ArgumentException)
                    {
                        addresses = Array.Empty<IPAddress>();
                    }
                    // Fail closed on DNS failure, including expiry of an earlier successful result.
                    Volatile.Write(ref cache, new(CreateForwarding(addresses), DateTimeOffset.UtcNow.AddSeconds(30)));
                }
            }
            finally { refresh.Release(); }
        }
        var current = Volatile.Read(ref cache).Middleware;
        if (current == null) await next(context);
        else await current.Invoke(context);
    }

    private ForwardedHeadersMiddleware CreateForwarding(IPAddress[] addresses)
    {
        if (addresses.Length == 0) return null;
        var options = new ForwardedHeadersOptions
        {
            // HAProxy appends the actual caller as the rightmost X-Forwarded-For entry.
            // Keep enrollment limits per caller without trusting any client-supplied prefix.
            ForwardedHeaders = ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedFor,
            ForwardLimit = 1
        };
        options.KnownIPNetworks.Clear();
        options.KnownProxies.Clear();
        foreach (var address in addresses)
        {
            options.KnownProxies.Add(address);
            if (address.AddressFamily == AddressFamily.InterNetwork)
                options.KnownProxies.Add(address.MapToIPv6());
            else if (address.IsIPv4MappedToIPv6)
                options.KnownProxies.Add(address.MapToIPv4());
        }
        return new ForwardedHeadersMiddleware(next, loggerFactory, Options.Create(options));
    }
}
