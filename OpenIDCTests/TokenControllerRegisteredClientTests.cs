using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using OpenIDC.Controllers;
using OpenIDC.Models;
using OpenIDC.Services;

namespace OpenIDCTests
{
    /// <summary>Token endpoint rules for user-registered clients (websites = confidential, apps = public).</summary>
    public class TokenControllerRegisteredClientTests
    {
        private const string RedirectUri = "https://client.example/callback";
        private const string Verifier = "verifier-0123456789-0123456789-0123456789-abcdef";

        private FakeRegisteredClientRepository _repo;
        private RegisteredClientService _service;
        private ClientStore _clientStore;

        [SetUp]
        public void SetUp()
        {
            _repo = new FakeRegisteredClientRepository();
            _service = new RegisteredClientService(_repo);
            _clientStore = new ClientStore(new ClientStoreTests.FakeRedisService(), _repo);
        }

        private static string S256(string verifier) =>
            Convert.ToBase64String(SHA256.HashData(Encoding.ASCII.GetBytes(verifier)))
                .TrimEnd('=').Replace('+', '-').Replace('/', '_');

        private TokenController Controller(AuthorizationCode code = null, RefreshToken refresh = null)
        {
            var controller = new TokenController(
                _clientStore, new SingleCodeStore(code), new StubTokenService(), new SingleRefreshStore(refresh),
                NullLogger<TokenController>.Instance);
            controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
            return controller;
        }

        private static AuthorizationCode PkceCode(string clientId) => new AuthorizationCode
        {
            Code = "code1",
            ClientId = clientId,
            Subject = "alice",
            RedirectUri = RedirectUri,
            ExpiresAt = DateTime.UtcNow.AddMinutes(5),
            CodeChallenge = S256(Verifier),
            CodeChallengeMethod = "S256",
        };

        private Task<RegisteredClientDto> Register(string type) =>
            _service.CreateAsync("alice", new ClientRegistrationRequest
            {
                DisplayName = "x", ClientType = type, RedirectUris = new() { RedirectUri },
            });

        [Test]
        public async Task RegisteredWebsiteCannotExchangeCodeWithPkceAlone()
        {
            var client = await Register("web");

            var result = await Controller(PkceCode(client.ClientId)).Token(new TokenRequest
            {
                GrantType = "authorization_code", Code = "code1", RedirectUri = RedirectUri,
                ClientId = client.ClientId, CodeVerifier = Verifier,
            });

            Assert.That(result, Is.InstanceOf<UnauthorizedObjectResult>());
        }

        [Test]
        public async Task RegisteredWebsiteExchangesCodeWithItsSecret()
        {
            var client = await Register("web");

            var result = await Controller(PkceCode(client.ClientId)).Token(new TokenRequest
            {
                GrantType = "authorization_code", Code = "code1", RedirectUri = RedirectUri,
                ClientId = client.ClientId, ClientSecret = client.ClientSecret, CodeVerifier = Verifier,
            });

            Assert.That(result, Is.InstanceOf<OkObjectResult>());
        }

        [Test]
        public async Task RegisteredAppExchangesCodeWithPkceAndNoSecret()
        {
            var client = await Register("app");

            var result = await Controller(PkceCode(client.ClientId)).Token(new TokenRequest
            {
                GrantType = "authorization_code", Code = "code1", RedirectUri = RedirectUri,
                ClientId = client.ClientId, CodeVerifier = Verifier,
            });

            Assert.That(result, Is.InstanceOf<OkObjectResult>());
        }

        [Test]
        public async Task RegisteredAppCanRefreshWithoutSecret()
        {
            var client = await Register("app");
            var refresh = new RefreshToken
            {
                Token = "rt1", ClientId = client.ClientId, Subject = "alice", ExpiresAt = DateTime.UtcNow.AddDays(1),
            };

            var result = await Controller(refresh: refresh).Token(new TokenRequest
            {
                GrantType = "refresh_token", RefreshToken = "rt1", ClientId = client.ClientId,
            });

            Assert.That(result, Is.InstanceOf<OkObjectResult>());
        }

        [Test]
        public async Task ConfidentialClientStillNeedsSecretToRefresh()
        {
            var client = await Register("web");
            var refresh = new RefreshToken
            {
                Token = "rt1", ClientId = client.ClientId, Subject = "alice", ExpiresAt = DateTime.UtcNow.AddDays(1),
            };

            var result = await Controller(refresh: refresh).Token(new TokenRequest
            {
                GrantType = "refresh_token", RefreshToken = "rt1", ClientId = client.ClientId,
            });

            Assert.That(result, Is.InstanceOf<UnauthorizedObjectResult>());
        }

        private class SingleCodeStore : IAuthorizationCodeStore
        {
            private readonly AuthorizationCode _code;
            public SingleCodeStore(AuthorizationCode code) => _code = code;
            public Task<string> CreateAuthorizationCodeAsync(AuthorizationCode authCode) => Task.FromResult(authCode.Code);
            public Task<AuthorizationCode> GetAuthorizationCodeAsync(string code) =>
                Task.FromResult(_code != null && code == _code.Code ? _code : null);
            public Task RevokeAuthorizationCodeAsync(string code) => Task.CompletedTask;
            public Task StoreAuthorizationCodeAsync(AuthorizationCode authCode) => Task.CompletedTask;
        }

        private class SingleRefreshStore : IRefreshTokenStore
        {
            private readonly RefreshToken _token;
            public SingleRefreshStore(RefreshToken token) => _token = token;
            public Task<string> CreateRefreshTokenAsync(RefreshToken refreshToken) => Task.FromResult("new-refresh");
            public Task<RefreshToken> GetRefreshTokenAsync(string token) =>
                Task.FromResult(_token != null && token == _token.Token ? _token : null);
            public Task RevokeRefreshTokenAsync(string token) => Task.CompletedTask;
        }

        private class StubTokenService : ITokenService
        {
            public string GenerateAccessToken(string subject, string clientId, List<string> scopes, Dictionary<string, string> claims) => "access";
            public string GenerateIdToken(string subject, string clientId, List<string> scopes, Dictionary<string, string> claims, string nonce) => "id";
            public System.Security.Claims.ClaimsPrincipal ValidateToken(string token) => null;
            public System.Security.Claims.ClaimsPrincipal ValidateIdToken(string token, string expectedClientId) => null;
            public object GetJsonWebKeySet() => new { };
        }
    }
}
