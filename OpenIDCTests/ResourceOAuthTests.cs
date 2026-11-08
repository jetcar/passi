using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using NUnit.Framework;
using OpenIDC.Controllers;
using OpenIDC.Helpers;
using OpenIDC.Models;
using OpenIDC.Services;

namespace OpenIDCTests;

public class ResourceOAuthTests
{
    private const string Verifier = "verifier-0123456789-0123456789-0123456789-abcdef";
    private const string Redirect = "https://client.example/callback";
    private const string Subject = "alice@passi.cloud";
    private ClientStoreTests.FakeRedisService _redis;
    private ClientStore _clients;
    private AuthorizationCodeStore _codes;
    private RefreshTokenStore _refresh;
    private TokenService _tokens;
    private X509Certificate2 _certificate;
    private OidcClient _client;
    private LogCapture<TokenController> _logs;

    [SetUp]
    public async Task SetUp()
    {
        _redis = new(); _clients = new(_redis); _codes = new(_redis); _refresh = new(_redis);
        using var rsa = RSA.Create(2048);
        _certificate = new CertificateRequest("CN=Passi test", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1)
            .CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1));
        _tokens = new(DynamicRegistrationTests.Settings(), new SigningCredentials(
            new X509SecurityKey(_certificate) { KeyId = _certificate.GetCertHashString(HashAlgorithmName.SHA256) }, SecurityAlgorithms.RsaSha256));
        _client = new() { ClientId = "test-public", IsDynamic = true, IsPublicClient = true, RequiresPkce = true,
            RedirectUris = new() { Redirect }, AllowedScopes = new() { "openid", "email", "profile" },
            ExpiresAt = DateTime.UtcNow.AddDays(2) };
        await _clients.CreateClientAsync(_client);
        _logs = new();
    }

    [TearDown] public void TearDown() => _certificate.Dispose();

    private TokenController Controller() => new(_clients, _codes, _tokens, _refresh, _logs)
    { ControllerContext = new() { HttpContext = new DefaultHttpContext() } };

    private async Task<TokenRequest> CodeRequest(string resource = ResourcePolicy.Finance, string scope = "openid email", string method = "S256")
    {
        var code = await _codes.CreateAuthorizationCodeAsync(new()
        {
            ClientId = _client.ClientId, Resource = resource, RedirectUri = Redirect, Subject = Subject,
            Scopes = scope.Split(' ').ToList(), Claims = new() { ["email"] = Subject },
            CodeChallenge = method == "S256" ? Base64UrlEncoder.Encode(SHA256.HashData(Encoding.ASCII.GetBytes(Verifier))) : Verifier,
            CodeChallengeMethod = method,
        });
        return new() { GrantType = "authorization_code", ClientId = _client.ClientId, Code = code,
            RedirectUri = Redirect, CodeVerifier = Verifier, Resource = resource };
    }

    private static JsonElement Body(IActionResult result) => JsonSerializer.SerializeToElement(((ObjectResult)result).Value);

    private JwtSecurityToken VerifyWithPublishedJwks(string serialized, string audience)
    {
        var jwks = new JsonWebKeySet(JsonSerializer.Serialize(_tokens.GetJsonWebKeySet()));
        var validation = new TokenValidationParameters
        {
            ValidIssuer = "https://passi.cloud/openidc", ValidAudience = audience,
            ValidateIssuer = true, ValidateAudience = true, ValidateLifetime = true,
            RequireExpirationTime = true, RequireSignedTokens = true, ValidateIssuerSigningKey = true,
            IssuerSigningKeys = jwks.GetSigningKeys(), ValidAlgorithms = new[] { "RS256" }, ClockSkew = TimeSpan.Zero,
        };
        new JwtSecurityTokenHandler().ValidateToken(serialized, validation, out var verified);
        var jwt = (JwtSecurityToken)verified;
        Assert.That(jwt.Header.Kid, Is.EqualTo(jwks.Keys.Single().Kid));
        Assert.That(jwks.Keys.Single().D, Is.Null);
        return jwt;
    }

    [Test]
    public async Task ResourceCodeProducesIndependentlyVerifiableJwtAndCannotBeReplayed()
    {
        var request = await CodeRequest();
        var response = Body(await Controller().Token(request));
        var jwt = VerifyWithPublishedJwks(response.GetProperty("access_token").GetString(), ResourcePolicy.Finance);
        Assert.That(jwt.Subject, Is.EqualTo(Subject));
        Assert.That(jwt.Audiences, Is.EqualTo(new[] { ResourcePolicy.Finance }));
        Assert.That(jwt.Payload["client_id"], Is.EqualTo(_client.ClientId));
        Assert.That(jwt.Payload["email"], Is.EqualTo(Subject));
        Assert.That(jwt.Payload["scope"], Is.EqualTo("openid email"));
        Assert.That(jwt.Payload.ContainsKey("email_verified"), Is.False);
        Assert.That(jwt.Id, Is.Not.Empty);
        Assert.That(jwt.IssuedAt, Is.EqualTo(DateTime.UtcNow).Within(TimeSpan.FromSeconds(5)));
        Assert.That(jwt.ValidTo, Is.EqualTo(jwt.IssuedAt.AddHours(1)).Within(TimeSpan.FromSeconds(1)));
        VerifyWithPublishedJwks(response.GetProperty("id_token").GetString(), _client.ClientId);
        Assert.That(Body(await Controller().Token(request)).GetProperty("error").GetString(), Is.EqualTo("invalid_grant"));
        foreach (var sensitive in new[] { request.Code, request.Code[..8], Verifier,
            response.GetProperty("access_token").GetString(), response.GetProperty("refresh_token").GetString() })
            Assert.That(_logs.Messages, Has.None.Contains(sensitive));
    }

    [TestCase(null)]
    [TestCase(ResourcePolicy.Finance)]
    public async Task RefreshRotatesAndRetainsClientResourceSubjectAndScopes(string resource)
    {
        var response = Body(await Controller().Token(await CodeRequest()));
        var oldToken = response.GetProperty("refresh_token").GetString();
        var request = new TokenRequest { GrantType = "refresh_token", ClientId = _client.ClientId, RefreshToken = oldToken, Resource = resource };
        var rotated = Body(await Controller().Token(request));
        var jwt = VerifyWithPublishedJwks(rotated.GetProperty("access_token").GetString(), ResourcePolicy.Finance);
        Assert.That(jwt.Subject, Is.EqualTo(Subject));
        Assert.That(jwt.Payload["scope"], Is.EqualTo("openid email"));
        Assert.That(jwt.Payload["client_id"], Is.EqualTo(_client.ClientId));
        Assert.That(jwt.Payload["email"], Is.EqualTo(Subject));
        var next = rotated.GetProperty("refresh_token").GetString();
        Assert.That(next, Is.Not.EqualTo(oldToken));
        Assert.That(Body(await Controller().Token(request)).GetProperty("error").GetString(), Is.EqualTo("invalid_grant"));
        var stored = _redis.Get<RefreshToken>("oidc:refresh:" + next);
        Assert.That(stored.Resource, Is.EqualTo(ResourcePolicy.Finance));
        Assert.That(stored.ExpiresAt, Is.EqualTo(_client.ExpiresAt));
        Assert.That(_logs.Messages, Has.None.Contains(oldToken));
    }

    [Test]
    public async Task OmittedTokenIndicatorInheritsResource()
    {
        var request = await CodeRequest(); request.Resource = null;
        var response = Body(await Controller().Token(request));
        VerifyWithPublishedJwks(response.GetProperty("access_token").GetString(), ResourcePolicy.Finance);
    }

    [TestCase("https://unknown.example/mcp")]
    [TestCase("")]
    public async Task UnknownTokenResourcesAreRejected(string resource)
    {
        var request = await CodeRequest(); request.Resource = resource;
        Assert.That(Body(await Controller().Token(request)).GetProperty("error").GetString(), Is.EqualTo("invalid_target"));
    }

    [Test]
    public async Task CannotAddResourceToLegacyCodeOrRefreshGrant()
    {
        var request = await CodeRequest(resource: null); request.Resource = ResourcePolicy.Finance;
        Assert.That(Body(await Controller().Token(request)).GetProperty("error").GetString(), Is.EqualTo("invalid_target"));
        var legacy = Body(await Controller().Token(await CodeRequest(resource: null)));
        VerifyWithPublishedJwks(legacy.GetProperty("access_token").GetString(), _client.ClientId);
        var refresh = new TokenRequest { GrantType = "refresh_token", ClientId = _client.ClientId,
            RefreshToken = legacy.GetProperty("refresh_token").GetString(), Resource = ResourcePolicy.Finance };
        Assert.That(Body(await Controller().Token(refresh)).GetProperty("error").GetString(), Is.EqualTo("invalid_target"));
    }

    [Test]
    public async Task CannotSwitchBetweenTwoAllowedResources()
    {
        var policy = new ResourcePolicy(DynamicRegistrationTests.Settings(new()
        { ["AppSetting:OAuthAllowedResources"] = ResourcePolicy.Finance + " https://other.example/mcp" }));
        var controller = new TokenController(_clients, _codes, _tokens, _refresh, _logs, policy)
        { ControllerContext = new() { HttpContext = new DefaultHttpContext() } };
        var request = await CodeRequest(); request.Resource = "https://other.example/mcp";
        Assert.That(Body(await controller.Token(request)).GetProperty("error").GetString(), Is.EqualTo("invalid_target"));
        var valid = Body(await Controller().Token(await CodeRequest()));
        var refresh = new TokenRequest { GrantType = "refresh_token", ClientId = _client.ClientId,
            RefreshToken = valid.GetProperty("refresh_token").GetString(), Resource = "https://other.example/mcp" };
        Assert.That(Body(await controller.Token(refresh)).GetProperty("error").GetString(), Is.EqualTo("invalid_target"));
    }

    [Test]
    public async Task OAuthOnlyEmailScopeReturnsNoIdToken()
    {
        var response = Body(await Controller().Token(await CodeRequest(scope: "email")));
        Assert.That(response.TryGetProperty("id_token", out _), Is.False);
        var jwt = VerifyWithPublishedJwks(response.GetProperty("access_token").GetString(), ResourcePolicy.Finance);
        Assert.That(jwt.Payload["email"], Is.EqualTo(Subject));
    }

    [Test]
    public async Task AccessTokenOmitsEmailWithoutEmailScope()
    {
        var response = Body(await Controller().Token(await CodeRequest(scope: "openid")));
        var jwt = VerifyWithPublishedJwks(response.GetProperty("access_token").GetString(), ResourcePolicy.Finance);
        Assert.That(jwt.Payload.ContainsKey("email"), Is.False);
    }

    [TestCase(null)]
    [TestCase("plain")]
    public async Task PlainOrUnspecifiedPkceMethodCannotExchange(string method)
    {
        Assert.That(Body(await Controller().Token(await CodeRequest(method: method))).GetProperty("error").GetString(), Is.EqualTo("invalid_grant"));
    }

    [Test]
    public async Task PublicCodeWithoutPkceIsRejectedEvenIfSecretIsProvided()
    {
        var code = await _codes.CreateAuthorizationCodeAsync(new() { ClientId = _client.ClientId, Subject = Subject, RedirectUri = Redirect });
        var request = new TokenRequest { GrantType = "authorization_code", ClientId = _client.ClientId, Code = code, RedirectUri = Redirect, ClientSecret = "anything" };
        Assert.That(Body(await Controller().Token(request)).GetProperty("error").GetString(), Is.EqualTo("invalid_grant"));
    }

    [Test]
    public async Task SeededConfidentialClientStillWorksWithoutPkceOrResource()
    {
        _client.IsDynamic = false; _client.IsPublicClient = false; _client.RequiresPkce = false; _client.ClientSecret = "existing-secret";
        await _clients.CreateClientAsync(_client);
        var code = await _codes.CreateAuthorizationCodeAsync(new() { ClientId = _client.ClientId, Subject = Subject, RedirectUri = Redirect,
            Scopes = new() { "openid", "email" }, Claims = new() { ["email"] = Subject } });
        var request = new TokenRequest { GrantType = "authorization_code", ClientId = _client.ClientId, Code = code, RedirectUri = Redirect, ClientSecret = "existing-secret" };
        var response = Body(await Controller().Token(request));
        VerifyWithPublishedJwks(response.GetProperty("access_token").GetString(), _client.ClientId);
    }

    [Test]
    public async Task RefreshCannotExpandScopes()
    {
        var response = Body(await Controller().Token(await CodeRequest(scope: "email")));
        var request = new TokenRequest { GrantType = "refresh_token", ClientId = _client.ClientId,
            RefreshToken = response.GetProperty("refresh_token").GetString(), Scope = "email profile" };
        Assert.That(Body(await Controller().Token(request)).GetProperty("error").GetString(), Is.EqualTo("invalid_scope"));
    }

    [Test]
    public async Task ReplayingRotatedRefreshRevokesItsSuccessor()
    {
        var response = Body(await Controller().Token(await CodeRequest()));
        var request = new TokenRequest { GrantType = "refresh_token", ClientId = _client.ClientId,
            RefreshToken = response.GetProperty("refresh_token").GetString() };
        var rotated = Body(await Controller().Token(request));
        Assert.That(Body(await Controller().Token(request)).GetProperty("error").GetString(), Is.EqualTo("invalid_grant"));
        request.RefreshToken = rotated.GetProperty("refresh_token").GetString();
        Assert.That(Body(await Controller().Token(request)).GetProperty("error").GetString(), Is.EqualTo("invalid_grant"));
    }

    internal sealed class LogCapture<T> : ILogger<T>
    {
        public List<string> Messages { get; } = new();
        public IDisposable BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel level, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter) => Messages.Add(formatter(state, exception));
    }
}
