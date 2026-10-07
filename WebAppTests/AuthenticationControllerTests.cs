using System;
using System.Collections.Generic;
using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;
using NUnit.Framework;
using RestSharp;
using Services;
using WebApiDto.Auth.Dto;
using WebApp.Controllers;
using WebApp.Services;

namespace WebAppTests
{
    public class AuthenticationControllerTests
    {
        [Test]
        public async Task LogInCallbackDisposesTheLoadedCertificate()
        {
            const string state = "state-1";
            const string nonce = "nonce-1";
            var cache = new FakeDistributedCache();
            await cache.SetStringAsync($"oauth:state:{state}", state);
            await cache.SetStringAsync($"oauth:nonce:{state}", nonce);
            await cache.SetStringAsync($"oauth:verifier:{state}", "verifier-1");
            await cache.SetStringAsync($"oauth:returnurl:{state}", "/");

            var cert = CreateCertificate();
            var trackingCert = new DisposeTrackingCertificate(cert.RawData);
            var sessionDto = new SessionMinDto
            {
                PublicCert = Convert.ToBase64String(cert.RawData),
                SignedHash = Sign(nonce, cert),
                ExpirationTime = DateTime.UtcNow.AddHours(1),
                RandomString = "unused",
            };
            var rest = new FakeRestClient(new RestResponse
            {
                StatusCode = HttpStatusCode.OK,
                IsSuccessStatusCode = true,
                ResponseStatus = ResponseStatus.Completed,
                Content = JsonConvert.SerializeObject(sessionDto),
            });
            var oidcClient = new FakeOidcClient();

            var services = new ServiceCollection()
                .AddSingleton<IAuthenticationService>(new FakeAuthenticationService())
                .BuildServiceProvider();

            var controller = new AuthenticationController(rest, NullLogger<AuthenticationController>.Instance, oidcClient, cache)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext
                    {
                        Request = { Scheme = "https", Host = new HostString("passi.example") },
                        RequestServices = services,
                    },
                },
            };

            var previousLoader = AuthenticationController.LoadCertificate;
            AuthenticationController.LoadCertificate = _ => trackingCert;
            try
            {
                await controller.LogInCallback("code-1", state);
            }
            finally
            {
                AuthenticationController.LoadCertificate = previousLoader;
            }

            // LogInCallback loads a certificate (to read NotBefore/NotAfter) that nothing else
            // owns; it must dispose it itself instead of leaking the native handle.
            Assert.That(trackingCert.WasDisposed, Is.True);
        }

        private static string Sign(string data, X509Certificate2 certificate)
        {
            using var sha512 = SHA512.Create();
            var hash = sha512.ComputeHash(Encoding.ASCII.GetBytes(data));
            using var rsaPrivateKey = certificate.GetRSAPrivateKey();
            var signedBytes = rsaPrivateKey.SignHash(hash, HashAlgorithmName.SHA512, RSASignaturePadding.Pkcs1);
            return Convert.ToBase64String(signedBytes);
        }

        private static X509Certificate2 CreateCertificate()
        {
            using var rsa = RSA.Create();
            var request = new CertificateRequest($"cn={Guid.NewGuid()}", rsa, HashAlgorithmName.SHA512, RSASignaturePadding.Pkcs1);
            return request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddYears(1));
        }

        // Lets the test observe whether LogInCallback disposes the certificate it loaded.
        private class DisposeTrackingCertificate : X509Certificate2
        {
            public bool WasDisposed { get; private set; }

            public DisposeTrackingCertificate(byte[] rawData) : base(rawData)
            {
            }

            protected override void Dispose(bool disposing)
            {
                WasDisposed = true;
                base.Dispose(disposing);
            }
        }

        private class FakeRestClient : IMyRestClient
        {
            private readonly RestResponse _response;

            public FakeRestClient(RestResponse response) => _response = response;

            public Task<RestResponse> ExecuteAsync(RestRequest request) => Task.FromResult(_response);
        }

        private class FakeOidcClient : IOidcClient
        {
            public Task<string> BuildAuthorizationUrlAsync(string redirectUri, string returnUrl, string state, string nonce, string codeVerifier) =>
                Task.FromResult("https://passi.example/authorize");

            public Task<OidcTokenResponse> ExchangeCodeForTokensAsync(string redirectUri, string code, string codeVerifier) =>
                Task.FromResult(new OidcTokenResponse { AccessToken = "access", IdToken = "id", RefreshToken = "refresh", ExpiresIn = 3600 });

            public Task<ClaimsPrincipal> ValidateTokensAsync(OidcTokenResponse tokenResponse)
            {
                var identity = new ClaimsIdentity(new[]
                {
                    new Claim("sub", "alice"),
                    new Claim("Thumbprint", "thumbprint-1"),
                    new Claim("sessionId", "session-1"),
                }, "oidc");
                return Task.FromResult(new ClaimsPrincipal(identity));
            }
        }

        private class FakeAuthenticationService : IAuthenticationService
        {
            public Task<AuthenticateResult> AuthenticateAsync(HttpContext context, string scheme) =>
                Task.FromResult(AuthenticateResult.NoResult());

            public Task ChallengeAsync(HttpContext context, string scheme, AuthenticationProperties properties) => Task.CompletedTask;
            public Task ForbidAsync(HttpContext context, string scheme, AuthenticationProperties properties) => Task.CompletedTask;
            public Task SignInAsync(HttpContext context, string scheme, ClaimsPrincipal principal, AuthenticationProperties properties) => Task.CompletedTask;
            public Task SignOutAsync(HttpContext context, string scheme, AuthenticationProperties properties) => Task.CompletedTask;
        }

        // Minimal in-memory IDistributedCache: AuthenticationController only ever reads/writes
        // string values via GetStringAsync/SetStringAsync/RemoveAsync.
        private class FakeDistributedCache : IDistributedCache
        {
            private readonly Dictionary<string, byte[]> _store = new();

            public byte[] Get(string key) => _store.TryGetValue(key, out var value) ? value : null;

            public Task<byte[]> GetAsync(string key, CancellationToken token = default) => Task.FromResult(Get(key));

            public void Refresh(string key)
            {
            }

            public Task RefreshAsync(string key, CancellationToken token = default) => Task.CompletedTask;

            public void Remove(string key) => _store.Remove(key);

            public Task RemoveAsync(string key, CancellationToken token = default)
            {
                Remove(key);
                return Task.CompletedTask;
            }

            public void Set(string key, byte[] value, DistributedCacheEntryOptions options) => _store[key] = value;

            public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default)
            {
                Set(key, value, options);
                return Task.CompletedTask;
            }
        }
    }
}
