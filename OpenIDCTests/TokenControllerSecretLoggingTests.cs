using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using OpenIDC.Controllers;
using OpenIDC.Models;
using OpenIDC.Services;

namespace OpenIDCTests
{
    public class TokenControllerSecretLoggingTests
    {
        private const string TheSecret = "super-secret-client-value";

        [Test]
        public async Task TokenNeverLogsClientSecretWhenValidationFails()
        {
            var clientStore = new FakeClientStore(new OidcClient { ClientId = "client1", ClientSecret = TheSecret });
            var authCodeStore = new FakeAuthorizationCodeStore(new AuthorizationCode
            {
                Code = "code1",
                ClientId = "client1",
                Subject = "subject1",
                RedirectUri = "https://client.example/callback",
                ExpiresAt = DateTime.UtcNow.AddMinutes(5)
            });
            var capturingLogger = new CapturingLogger<TokenController>();
            var controller = new TokenController(clientStore, authCodeStore, new FakeTokenService(), new FakeRefreshTokenStore(), capturingLogger);

            var request = new TokenRequest
            {
                GrantType = "authorization_code",
                Code = "code1",
                RedirectUri = "https://client.example/callback",
                ClientId = "client1",
                ClientSecret = "wrong-" + TheSecret
            };

            var result = await controller.Token(request);

            Assert.That(result, Is.InstanceOf<UnauthorizedObjectResult>());
            Assert.That(capturingLogger.CapturedMessages, Has.None.Contains(TheSecret));
            Assert.That(capturingLogger.CapturedMessages, Has.None.Contains("wrong-" + TheSecret));
        }

        private class FakeClientStore : IClientStore
        {
            private readonly OidcClient _client;

            public FakeClientStore(OidcClient client) => _client = client;

            public Task<OidcClient> FindByClientIdAsync(string clientId) =>
                Task.FromResult(clientId == _client.ClientId ? _client : null);

            public Task<OidcClient> GetClientAsync(string clientId) => FindByClientIdAsync(clientId);

            public Task CreateClientAsync(OidcClient client) => Task.CompletedTask;

            public Task DeleteClientAsync(string clientId) => Task.CompletedTask;

            public Task<bool> ValidateClientAsync(string clientId, string clientSecret) =>
                Task.FromResult(clientId == _client.ClientId && clientSecret == _client.ClientSecret);
        }

        private class FakeAuthorizationCodeStore : IAuthorizationCodeStore
        {
            private readonly AuthorizationCode _authCode;

            public FakeAuthorizationCodeStore(AuthorizationCode authCode) => _authCode = authCode;

            public Task<string> CreateAuthorizationCodeAsync(AuthorizationCode authCode) => Task.FromResult(authCode.Code);

            public Task<AuthorizationCode> GetAuthorizationCodeAsync(string code) =>
                Task.FromResult(code == _authCode.Code ? _authCode : null);

            public Task RevokeAuthorizationCodeAsync(string code) => Task.CompletedTask;

            public Task StoreAuthorizationCodeAsync(AuthorizationCode authCode) => Task.CompletedTask;
        }

        private class FakeTokenService : ITokenService
        {
            public string GenerateAccessToken(string subject, string clientId, List<string> scopes, Dictionary<string, string> claims) => "access-token";
            public string GenerateIdToken(string subject, string clientId, List<string> scopes, Dictionary<string, string> claims, string nonce) => "id-token";
            public System.Security.Claims.ClaimsPrincipal ValidateToken(string token) => null;
            public System.Security.Claims.ClaimsPrincipal ValidateIdToken(string token, string expectedClientId) => null;
            public object GetJsonWebKeySet() => new { };
        }

        private class FakeRefreshTokenStore : IRefreshTokenStore
        {
            public Task<string> CreateRefreshTokenAsync(RefreshToken refreshToken) => Task.FromResult("refresh-token");
            public Task<RefreshToken> GetRefreshTokenAsync(string token) => Task.FromResult<RefreshToken>(null);
            public Task RevokeRefreshTokenAsync(string token) => Task.CompletedTask;
        }

        // Captures every rendered log message (template + args) so tests can assert
        // that sensitive values never reach the log output.
        private class CapturingLogger<T> : ILogger<T>
        {
            public List<string> CapturedMessages { get; } = new List<string>();

            public IDisposable BeginScope<TState>(TState state) where TState : notnull => null;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
            {
                CapturedMessages.Add(formatter(state, exception));
            }
        }
    }
}
