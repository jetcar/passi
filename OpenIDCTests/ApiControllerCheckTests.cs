using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using ConfigurationManager;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using OpenIDC.Controllers;
using OpenIDC.Models;
using OpenIDC.Services;
using RestSharp;
using Services;
using WebApiDto;
using WebApiDto.Auth;
using WebApiDto.Auth.Dto;

namespace OpenIDCTests
{
    public class ApiControllerCheckTests
    {
        private const string RedirectUri = "https://site/cb";
        private const string ClientId = "SampleApp";

        [Test]
        public async Task CheckRejectsAnExpiredCertificate()
        {
            var result = await RunCheck(MakeSelfSignedCert(notBefore: DateTimeOffset.UtcNow.AddDays(-2), notAfter: DateTimeOffset.UtcNow.AddDays(-1)));

            var badRequest = (BadRequestObjectResult)result;
            Assert.That(((ApiResponseDto)badRequest.Value).errors, Is.EqualTo("Invalid Certificate"));
        }

        [Test]
        public async Task CheckRejectsANotYetValidCertificate()
        {
            var result = await RunCheck(MakeSelfSignedCert(notBefore: DateTimeOffset.UtcNow.AddDays(1), notAfter: DateTimeOffset.UtcNow.AddDays(2)));

            var badRequest = (BadRequestObjectResult)result;
            Assert.That(((ApiResponseDto)badRequest.Value).errors, Is.EqualTo("Invalid Certificate"));
        }

        [Test]
        public async Task CheckContinuesPastAValidCertificate()
        {
            // A currently-valid certificate should not be rejected by the expiry check;
            // the flow proceeds to the (mocked) signature verification step below it,
            // which then fails on the deliberately-wrong signature - proving we got past
            // the certificate-validity check rather than being stopped by it.
            var result = await RunCheck(MakeSelfSignedCert(notBefore: DateTimeOffset.UtcNow.AddDays(-1), notAfter: DateTimeOffset.UtcNow.AddDays(1)));

            var badRequest = (BadRequestObjectResult)result;
            Assert.That(((ApiResponseDto)badRequest.Value).errors, Is.EqualTo("Invalid Signature"));
        }

        [Test]
        public async Task CheckWithoutNonceVerifiesTheSessionRandomString()
        {
            // Plain OAuth clients (e.g. MCP agents such as Claude Code) send no nonce. The phone signed the
            // session's server-generated random string, so the check must verify against that.
            var cert = MakeSelfSignedCert(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1), out var rsa);
            var signed = Sign("5551234567", rsa);

            var result = await RunCheck(cert, new SessionMinDto { SignedHash = signed, RandomString = "5551234567" }, nonce: null,
                state: "abc", codeChallenge: "challenge");

            var ok = (OkObjectResult)result;
            var redirect = (string)ok.Value.GetType().GetProperty("redirect_url").GetValue(ok.Value);
            Assert.That(redirect, Does.StartWith(RedirectUri + "?code="));
            Assert.That(redirect, Does.EndWith("&state=abc"));
        }

        [Test]
        public async Task CheckIgnoresABrowserSuppliedNonceThatDiffersFromTheSignedRandomString()
        {
            var cert = MakeSelfSignedCert(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1), out var rsa);
            var signed = Sign("5551234567", rsa);

            var result = await RunCheck(cert, new SessionMinDto { SignedHash = signed, RandomString = "5551234567" }, nonce: "attacker-chosen");

            Assert.That(result, Is.InstanceOf<OkObjectResult>());
        }

        [Test]
        public async Task CheckReturnsBadRequestInsteadOfThrowingWhenCertificateLookupReturnsNull()
        {
            // The internal api/Certificate/Public call can come back HTTP-successful with an empty/null
            // body (e.g. the lookup finding no matching record). Check must reject that cleanly instead
            // of dereferencing a null CertificateDto.
            var rest = new SequencedRestClient(new[]
            {
                // 1. checkRequest
                MakeJsonResponse(new CheckResponceDto { Username = "alice@passi.cloud", PublicCertThumbprint = "thumb" }),
                // 2. api/Certificate/Public - successful response, but with no certificate data
                MakeJsonResponse<CertificateDto>(null),
            });

            var appSetting = new AppSetting(new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string> { ["AppSetting:checkRequest"] = "/api/auth/check" })
                .Build())
            { PrefferAppsettingFile = true };

            var clientStore = new FixedClientStore(new OidcClient
            {
                ClientId = ClientId,
                RedirectUris = new List<string> { RedirectUri },
            });

            var controller = new ApiController(new FixedRandom(), rest, appSetting, NullLogger<ApiController>.Instance, clientStore, new FakeAuthCodeStore());

            var result = await controller.Check(sessionId: "session", nonce: "nonce", client_id: ClientId, redirect_uri: RedirectUri,
                scope: null, state: null, code_challenge: null, code_challenge_method: null);

            var badRequest = (BadRequestObjectResult)result;
            Assert.That(((ApiResponseDto)badRequest.Value).errors, Is.EqualTo("Invalid Certificate"));
        }

        [Test]
        public async Task CheckReturnsBadRequestInsteadOfThrowingWhenSessionLookupReturnsNull()
        {
            // The internal api/Auth/session call can come back HTTP-successful with an empty/null body
            // too (e.g. the session having already expired out of the store). Check must reject that
            // cleanly instead of dereferencing a null SessionMinDto.
            var cert = MakeSelfSignedCert(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1));

            var result = await RunCheck(cert, session: null, nonce: "nonce");

            var badRequest = (BadRequestObjectResult)result;
            Assert.That(((ApiResponseDto)badRequest.Value).errors, Is.EqualTo("Invalid Signature"));
        }

        [Test]
        public async Task CheckDisposesTheLoadedPublicCertificate()
        {
            var cert = MakeSelfSignedCert(notBefore: DateTimeOffset.UtcNow.AddDays(-2), notAfter: DateTimeOffset.UtcNow.AddDays(-1));
            var trackingCert = new DisposeTrackingCertificate(cert.RawData);
            var previousLoader = ApiController.LoadCertificateFromPem;
            ApiController.LoadCertificateFromPem = _ => trackingCert;
            try
            {
                await RunCheck(cert);
            }
            finally
            {
                ApiController.LoadCertificateFromPem = previousLoader;
            }

            // Check loads a certificate purely to read its validity window; nothing else owns
            // it, so it must dispose it instead of leaking the native handle.
            Assert.That(trackingCert.WasDisposed, Is.True);
        }

        [Test]
        public async Task CheckWithNoRandomStringAnywhereIsARejectionNotACrash()
        {
            var cert = MakeSelfSignedCert(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1), out var rsa);

            var result = await RunCheck(cert, new SessionMinDto { SignedHash = Sign("x", rsa) }, nonce: null);

            var badRequest = (BadRequestObjectResult)result;
            Assert.That(((ApiResponseDto)badRequest.Value).errors, Is.EqualTo("Invalid Signature"));
        }

        private static string Sign(string data, RSA rsa)
        {
            var hash = SHA512.HashData(System.Text.Encoding.ASCII.GetBytes(data));
            return Convert.ToBase64String(rsa.SignHash(hash, HashAlgorithmName.SHA512, RSASignaturePadding.Pkcs1));
        }

        private static Task<IActionResult> RunCheck(X509Certificate2 cert) =>
            RunCheck(cert, new SessionMinDto { SignedHash = Convert.ToBase64String(new byte[64]) }, nonce: "nonce");

        private static async Task<IActionResult> RunCheck(X509Certificate2 cert, SessionMinDto session, string nonce,
            string state = null, string codeChallenge = null)
        {
            var publicCertBase64 = Convert.ToBase64String(cert.RawData);

            var rest = new SequencedRestClient(new[]
            {
                // 1. checkRequest
                MakeJsonResponse(new CheckResponceDto { Username = "alice@passi.cloud", PublicCertThumbprint = "thumb" }),
                // 2. api/Certificate/Public
                MakeJsonResponse(new CertificateDto { Thumbprint = "thumb", PublicCert = publicCertBase64 }),
                // 3. api/Auth/session - only reached if the certificate passes the validity check
                MakeJsonResponse(session),
            });

            var appSetting = new AppSetting(new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string> { ["AppSetting:checkRequest"] = "/api/auth/check" })
                .Build())
            { PrefferAppsettingFile = true };

            var clientStore = new FixedClientStore(new OidcClient
            {
                ClientId = ClientId,
                RedirectUris = new List<string> { RedirectUri },
            });

            var controller = new ApiController(new FixedRandom(), rest, appSetting, NullLogger<ApiController>.Instance, clientStore, new FakeAuthCodeStore());

            return await controller.Check(sessionId: "session", nonce: nonce, client_id: ClientId, redirect_uri: RedirectUri,
                scope: null, state: state, code_challenge: codeChallenge, code_challenge_method: codeChallenge == null ? null : "S256");
        }

        private static RestResponse MakeJsonResponse<T>(T value)
        {
            return new RestResponse(new RestRequest())
            {
                StatusCode = HttpStatusCode.OK,
                IsSuccessStatusCode = true,
                ResponseStatus = ResponseStatus.Completed,
                Content = Newtonsoft.Json.JsonConvert.SerializeObject(value),
            };
        }

        private static X509Certificate2 MakeSelfSignedCert(DateTimeOffset notBefore, DateTimeOffset notAfter) =>
            MakeSelfSignedCert(notBefore, notAfter, out _);

        private static X509Certificate2 MakeSelfSignedCert(DateTimeOffset notBefore, DateTimeOffset notAfter, out RSA rsa)
        {
            rsa = RSA.Create(2048);
            var request = new CertificateRequest("CN=night-agent-test", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            return request.CreateSelfSigned(notBefore, notAfter);
        }

        private class FixedRandom : IRandomGenerator
        {
            public string GetNumbersString(int i) => new string('1', i);
        }

        private class FixedClientStore : IClientStore
        {
            private readonly OidcClient _client;

            public FixedClientStore(OidcClient client) => _client = client;

            public Task<OidcClient> FindByClientIdAsync(string clientId) => Task.FromResult(clientId == _client.ClientId ? _client : null);
            public Task<OidcClient> GetClientAsync(string clientId) => FindByClientIdAsync(clientId);
            public Task CreateClientAsync(OidcClient client) => Task.CompletedTask;
            public Task DeleteClientAsync(string clientId) => Task.CompletedTask;
            public Task<bool> ValidateClientAsync(string clientId, string clientSecret) => Task.FromResult(true);
        }

        private class FakeAuthCodeStore : IAuthorizationCodeStore
        {
            public Task<string> CreateAuthorizationCodeAsync(AuthorizationCode authCode) => Task.FromResult("code123");
            public Task<AuthorizationCode> ConsumeAuthorizationCodeAsync(string code) => Task.FromResult<AuthorizationCode>(null);
        }

        private class SequencedRestClient : IMyRestClient
        {
            private readonly Queue<RestResponse> _responses;

            public SequencedRestClient(IEnumerable<RestResponse> responses) => _responses = new Queue<RestResponse>(responses);

            public Task<RestResponse> ExecuteAsync(RestRequest request) => Task.FromResult(_responses.Dequeue());
        }

        // Lets the test observe whether Check disposes the certificate it loaded.
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
    }
}
