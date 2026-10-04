using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using OpenIDC.Controllers;
using OpenIDC.Models;
using OpenIDC.Services;

namespace OpenIDCTests
{
    /// <summary>
    /// A rejected refresh logs out the MCP client, so the server must say why (unknown token, expired, wrong
    /// client) without writing the refresh token itself to the logs.
    /// </summary>
    public class TokenControllerRefreshLoggingTests
    {
        private const string RefreshTokenValue = "0123456789abcdef0123456789abcdef";

        private static (TokenController controller, CapturingLogger<TokenController> logger) Create(RefreshToken stored)
        {
            var logger = new CapturingLogger<TokenController>();
            var controller = new TokenController(
                new PublicClientStore("mcp-client"),
                new NoAuthorizationCodes(),
                new FakeTokenService(),
                new SingleRefreshTokenStore(stored),
                logger)
            {
                ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() },
            };
            return (controller, logger);
        }

        private static TokenRequest RefreshRequest(string clientId = "mcp-client") => new TokenRequest
        {
            GrantType = "refresh_token",
            ClientId = clientId,
            RefreshToken = RefreshTokenValue,
        };

        [Test]
        public async Task UnknownRefreshTokenIsLoggedAsNotFound()
        {
            var (controller, logger) = Create(stored: null);

            var result = await controller.Token(RefreshRequest());

            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
            Assert.That(logger.Warnings, Has.Some.Contains("not found").And.Contains("mcp-client"));
            Assert.That(logger.All, Has.None.Contains(RefreshTokenValue));
        }

        [Test]
        public async Task ExpiredRefreshTokenIsLoggedAsExpired()
        {
            var (controller, logger) = Create(new RefreshToken
            {
                Token = RefreshTokenValue, ClientId = "mcp-client", Subject = "admin@passi.cloud", ExpiresAt = DateTime.UtcNow.AddMinutes(-1),
            });

            var result = await controller.Token(RefreshRequest());

            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
            Assert.That(logger.Warnings, Has.Some.Contains("expired").And.Contains("mcp-client"));
            Assert.That(logger.All, Has.None.Contains(RefreshTokenValue));
        }

        [Test]
        public async Task RefreshTokenOfAnotherClientIsLoggedAsClientMismatch()
        {
            var (controller, logger) = Create(new RefreshToken
            {
                Token = RefreshTokenValue, ClientId = "other-client", Subject = "admin@passi.cloud", ExpiresAt = DateTime.UtcNow.AddDays(1),
            });

            var result = await controller.Token(RefreshRequest());

            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
            Assert.That(logger.Warnings, Has.Some.Contains("mismatch").And.Contains("other-client"));
            Assert.That(logger.All, Has.None.Contains(RefreshTokenValue));
        }

        private class PublicClientStore : IClientStore
        {
            private readonly OidcClient _client;

            public PublicClientStore(string clientId) =>
                _client = new OidcClient { ClientId = clientId, IsPublicClient = true };

            public Task<OidcClient> FindByClientIdAsync(string clientId) =>
                Task.FromResult(clientId == _client.ClientId ? _client : null);

            public Task<OidcClient> GetClientAsync(string clientId) => FindByClientIdAsync(clientId);

            public Task CreateClientAsync(OidcClient client) => Task.CompletedTask;

            public Task DeleteClientAsync(string clientId) => Task.CompletedTask;

            public Task<bool> ValidateClientAsync(string clientId, string clientSecret) => Task.FromResult(false);
        }

        private class NoAuthorizationCodes : IAuthorizationCodeStore
        {
            public Task<string> CreateAuthorizationCodeAsync(AuthorizationCode authCode) => Task.FromResult(authCode.Code);

            public Task<AuthorizationCode> ConsumeAuthorizationCodeAsync(string code) => Task.FromResult<AuthorizationCode>(null);
        }

        private class FakeTokenService : ITokenService
        {
            public string GenerateAccessToken(string subject, string clientId, List<string> scopes, Dictionary<string, string> claims) => "access-token";
            public string GenerateIdToken(string subject, string clientId, List<string> scopes, Dictionary<string, string> claims, string nonce) => "id-token";
            public System.Security.Claims.ClaimsPrincipal ValidateToken(string token) => null;
            public System.Security.Claims.ClaimsPrincipal ValidateIdToken(string token, string expectedClientId) => null;
            public object GetJsonWebKeySet() => new { };
        }

        private class SingleRefreshTokenStore : IRefreshTokenStore
        {
            private RefreshToken _stored;

            public SingleRefreshTokenStore(RefreshToken stored) => _stored = stored;

            public Task<string> CreateRefreshTokenAsync(RefreshToken refreshToken) => Task.FromResult("new-refresh-token");

            public Task<RefreshToken> ConsumeRefreshTokenAsync(string token)
            {
                var found = _stored?.Token == token ? _stored : null;
                _stored = null;
                return Task.FromResult(found);
            }
        }

        private class CapturingLogger<T> : ILogger<T>
        {
            private readonly List<(LogLevel Level, string Message)> _entries = new();

            public IEnumerable<string> All => _entries.Select(e => e.Message);
            public IEnumerable<string> Warnings => _entries.Where(e => e.Level == LogLevel.Warning).Select(e => e.Message);

            public IDisposable BeginScope<TState>(TState state) where TState : notnull => null;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter) =>
                _entries.Add((logLevel, formatter(state, exception)));
        }
    }
}
