using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using ConfigurationManager;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using OpenIDC.Controllers;
using OpenIDC.Helpers;
using OpenIDC.Models;
using OpenIDC.Services;
using RedisClient;
using ServiceStack.Redis;

namespace OpenIDCTests;

public class DynamicRegistrationTests
{
    internal static AppSetting Settings(Dictionary<string, string> extra = null)
    {
        var values = extra ?? new();
        values.TryAdd("AppSetting:IdentityUrlBase", "https://passi.cloud");
        return new AppSetting(new ConfigurationBuilder().AddInMemoryCollection(values).Build()) { PrefferAppsettingFile = true };
    }

    internal static DynamicRegistrationRequest Request() => new()
    {
        ClientName = "<img src=x onerror=alert(1)>", RedirectUris = new() { "http://127.0.0.1:4242/callback" },
    };

    [TestCase("https://client.example/cb?registered=1")]
    [TestCase("http://127.0.0.1:4242/callback")]
    [TestCase("http://[::1]:4242/callback")]
    [TestCase("http://localhost:4242/callback")]
    public void AcceptsSafeRedirects(string uri)
    {
        var request = Request(); request.RedirectUris = new() { uri };
        Assert.That(DynamicClientRegistration.Validate(request), Is.Null);
    }

    [TestCase("http://example.com/cb")]
    [TestCase("http://localhost.evil.example/cb")]
    [TestCase("http://192.168.1.1/cb")]
    [TestCase("https://*.example.com/cb")]
    [TestCase("https://example.com/*")]
    [TestCase("https://example.com/cb#fragment")]
    [TestCase("https://example.com/cb#")]
    [TestCase("https://user:pass@example.com/cb")]
    [TestCase("https://user@example.com/cb")]
    [TestCase("javascript:alert(1)")]
    [TestCase("//example.com/cb")]
    [TestCase("https://example.com/\\evil")]
    [TestCase(" https://example.com/cb")]
    [TestCase("https://example.com/%zz")]
    [TestCase(null)]
    public void RejectsUnsafeRedirects(string uri)
    {
        var request = Request(); request.RedirectUris = new() { uri };
        Assert.That(DynamicClientRegistration.Validate(request), Is.EqualTo("invalid_redirect_uri"));
    }

    [TestCase("{\"token_endpoint_auth_method\":\"client_secret_post\"}")]
    [TestCase("{\"token_endpoint_auth_method\":\"private_key_jwt\"}")]
    [TestCase("{\"client_secret\":\"secret\"}")]
    [TestCase("{\"grant_types\":[\"client_credentials\"]}")]
    [TestCase("{\"grant_types\":[\"authorization_code\"]}")]
    [TestCase("{\"response_types\":[\"token\"]}")]
    [TestCase("{\"jwks_uri\":\"https://internal.example/keys\"}")]
    [TestCase("{\"scope\":\"admin\"}")]
    public void RejectsUnsupportedClientSettings(string json)
    {
        var request = JsonSerializer.Deserialize<DynamicRegistrationRequest>(json);
        request.RedirectUris = Request().RedirectUris;
        Assert.That(DynamicClientRegistration.Validate(request), Is.EqualTo("invalid_client_metadata"));
    }

    [Test]
    public void UnknownDescriptiveUrlsAreDiscarded()
    {
        var request = JsonSerializer.Deserialize<DynamicRegistrationRequest>(
            "{\"client_uri\":\"http://169.254.169.254/\",\"logo_uri\":\"http://localhost/secret\"}");
        request.RedirectUris = Request().RedirectUris;
        Assert.That(DynamicClientRegistration.Validate(request), Is.Null);
    }

    [Test]
    public void DynamicRedirectComparisonIsOrdinalAndIncludesPortQueryAndFragment()
    {
        var client = new OidcClient { IsDynamic = true, IsPublicClient = true,
            RedirectUris = new() { "http://localhost:4242/cb?registered=1" } };
        Assert.That(RedirectUriHelper.IsAllowed(client.RedirectUris[0], client), Is.True);
        foreach (var value in new[] { "http://localhost:9999/cb?registered=1", "http://localhost:4242/cb?registered=2",
            "http://LOCALHOST:4242/cb?registered=1", "http://localhost:4242/cb?registered=1#fragment" })
            Assert.That(RedirectUriHelper.IsAllowed(value, client), Is.False);
    }
}

// Run against a disposable Redis: PASSI_TEST_REDIS_PORT=16389. No production connections or FLUSH commands.
[NonParallelizable]
public class DynamicRegistrationRedisTests
{
    private RedisService _redis;
    private string _suffix;

    [SetUp]
    public void SetUp()
    {
        var port = Environment.GetEnvironmentVariable("PASSI_TEST_REDIS_PORT");
        if (port == null) Assert.Ignore("Set PASSI_TEST_REDIS_PORT to a disposable local Redis port.");
        _redis = new RedisService(DynamicRegistrationTests.Settings(new()
        {
            ["AppSetting:redis"] = "127.0.0.1", ["AppSetting:redisPort"] = port,
        }));
        _suffix = Guid.NewGuid().ToString("N");
    }

    [Test]
    public async Task RegistersPublicClientAndReadsRealRedisEncoding()
    {
        var service = new DynamicClientRegistration(_redis, DynamicRegistrationTests.Settings());
        var controller = new RegistrationController(service, NullLogger<RegistrationController>.Instance)
        { ControllerContext = new() { HttpContext = new DefaultHttpContext() } };
        controller.HttpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("192.0.2.25");
        var response = (ObjectResult)controller.Register(DynamicRegistrationTests.Request());
        Assert.That(response.StatusCode, Is.EqualTo(201));
        var body = JsonSerializer.SerializeToElement(response.Value);
        Assert.That(body.TryGetProperty("client_secret", out _), Is.False);
        Assert.That(body.GetProperty("token_endpoint_auth_method").GetString(), Is.EqualTo("none"));
        var id = body.GetProperty("client_id").GetString();
        Assert.That(id, Does.StartWith("mcp_").And.Length.EqualTo(47));
        var stored = await new ClientStore(_redis).GetClientAsync(id);
        Assert.That(stored.IsPublicClient && stored.IsDynamic && stored.RequiresPkce, Is.True);
        Assert.That(stored.ClientSecret, Is.Null);
        Assert.That(stored.ClientSecretHash, Is.Null);
        Assert.That(stored.ExpiresAt, Is.GreaterThan(DateTime.UtcNow));
        Assert.That(stored.DisplayName, Is.EqualTo(DynamicRegistrationTests.Request().ClientName));
        Assert.That(await new ClientStore(_redis).ValidateClientAsync(id, ""), Is.False);
    }

    [Test]
    public async Task CapacityIsAtomicAcrossConnectionsAndExpiredEntriesAreCleaned()
    {
        var bucket = "test:bounded:" + _suffix;
        var added = await Task.WhenAll(Enumerable.Range(0, 12).Select(i => Task.Run(() =>
            _redis.TryAddBounded(bucket, bucket + i, new OidcClient { ClientId = i.ToString() }, 2, TimeSpan.FromMilliseconds(800)))));
        Assert.That(added.Count(x => x), Is.EqualTo(2));
        await Task.Delay(1000);
        Assert.That(Enumerable.Range(0, 12).Select(i => _redis.Get<OidcClient>(bucket + i)), Has.All.Null);
        Assert.That(_redis.TryAddBounded(bucket, bucket + "new", new OidcClient(), 2, TimeSpan.FromMilliseconds(100)), Is.True);
        await Task.Delay(200);
        using var raw = new ServiceStack.Redis.RedisClient("127.0.0.1", int.Parse(Environment.GetEnvironmentVariable("PASSI_TEST_REDIS_PORT")));
        Assert.That(raw.ContainsKey(bucket), Is.False, "Idle index must expire too");
    }

    [Test]
    public async Task RegistrationRateWindowIsSharedAndExpires()
    {
        var key = "test:registration-rate:" + _suffix;
        Assert.That(_redis.TryAcquireFixedWindow(key, 1, TimeSpan.FromMilliseconds(100), out _), Is.True);
        Assert.That(_redis.TryAcquireFixedWindow(key, 1, TimeSpan.FromMilliseconds(100), out var retry), Is.False);
        Assert.That(retry, Is.GreaterThan(TimeSpan.Zero));
        await Task.Delay(200);
        Assert.That(_redis.TryAcquireFixedWindow(key, 1, TimeSpan.FromMilliseconds(100), out _), Is.True);
    }

    [Test]
    public async Task ConcurrentRealRedisRefreshReplayInvalidatesSuccessors()
    {
        var store = new RefreshTokenStore(_redis);
        var token = await store.CreateRefreshTokenAsync(new RefreshToken
        { ClientId = "mcp_test", Subject = "alice", Resource = ResourcePolicy.Finance });
        var results = await Task.WhenAll(
            Task.Run(() => store.ConsumeRefreshTokenAsync(token)),
            Task.Run(() => store.ConsumeRefreshTokenAsync(token)));
        Assert.That(results.Count(x => x != null), Is.LessThanOrEqualTo(1));
        foreach (var grant in results.Where(x => x != null))
        {
            Assert.That(grant.Resource, Is.EqualTo(ResourcePolicy.Finance));
            var successor = await store.CreateRefreshTokenAsync(grant);
            Assert.That(await store.ConsumeRefreshTokenAsync(successor), Is.Null);
        }
    }
}
