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

        private static async Task<IActionResult> RunCheck(X509Certificate2 cert)
        {
            var publicCertBase64 = Convert.ToBase64String(cert.RawData);

            var rest = new SequencedRestClient(new[]
            {
                // 1. checkRequest
                MakeJsonResponse(new CheckResponceDto { Username = "alice@passi.cloud", PublicCertThumbprint = "thumb" }),
                // 2. api/Certificate/Public
                MakeJsonResponse(new CertificateDto { Thumbprint = "thumb", PublicCert = publicCertBase64 }),
                // 3. api/Auth/session - only reached if the certificate passes the validity check
                MakeJsonResponse(new SessionMinDto { SignedHash = Convert.ToBase64String(new byte[64]) }),
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

            var controller = new ApiController(new FixedRandom(), rest, appSetting, NullLogger<ApiController>.Instance, clientStore, null);

            return await controller.Check(sessionId: "session", nonce: "nonce", client_id: ClientId, redirect_uri: RedirectUri,
                scope: null, state: null, code_challenge: null, code_challenge_method: null);
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

        private static X509Certificate2 MakeSelfSignedCert(DateTimeOffset notBefore, DateTimeOffset notAfter)
        {
            using var rsa = RSA.Create(2048);
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

        private class SequencedRestClient : IMyRestClient
        {
            private readonly Queue<RestResponse> _responses;

            public SequencedRestClient(IEnumerable<RestResponse> responses) => _responses = new Queue<RestResponse>(responses);

            public Task<RestResponse> ExecuteAsync(RestRequest request) => Task.FromResult(_responses.Dequeue());
        }
    }
}
