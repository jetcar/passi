using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using ConfigurationManager;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using OpenIDC.Models;
using OpenIDC.Services;
using RestSharp;
using Services;
using WebApiDto;
using WebApiDto.Auth;

namespace OpenIDCTests
{
    public class ApiControllerLoginTests
    {
        private CapturingRestClient _rest;

        private ApiController Controller(
            OidcClient client = null,
            ILoginRateLimiter loginRateLimiter = null,
            ILoginStartTokenService loginStartTokenService = null)
        {
            _rest = new CapturingRestClient();
            var appSetting = new AppSetting(new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string> { ["AppSetting:startRequest"] = "/api/auth/start" })
                .Build()) { PrefferAppsettingFile = true };

            client ??= Client();
            var controller = new ApiController(new FixedRandom(), _rest, appSetting, NullLogger<ApiController>.Instance,
                new FixedClientStore(client), null, loginRateLimiter ?? new FixedLoginRateLimiter(true),
                loginStartTokenService ?? new FixedLoginStartTokenService(true));
            controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
            controller.HttpContext.Connection.RemoteIpAddress = IPAddress.Parse("192.0.2.10");
            controller.Request.Headers["Sec-Fetch-Site"] = "same-origin";
            return controller;
        }

        private static OidcClient Client(
            string clientId = "SampleApp",
            string redirectUri = "https://site/cb",
            bool requiresPkce = false) => new()
        {
            ClientId = clientId,
            RedirectUris = new List<string> { redirectUri },
            AllowedScopes = new List<string> { "openid", "profile", "email" },
            RequiresPkce = requiresPkce,
            IsPublicClient = requiresPkce,
        };

        private static Task<IActionResult> Login(
            ApiController controller,
            string clientId = "SampleApp",
            string redirectUri = "https://site/cb",
            string responseType = "code",
            string scope = "openid profile email",
            string state = "state",
            string codeChallenge = null,
            string codeChallengeMethod = null) =>
            controller.Login(redirectUri, "nonce", "alice@passi.cloud", null, clientId, responseType, scope, state,
                codeChallenge, codeChallengeMethod, "login-token");

        [Test]
        public async Task LoginSendsTwoDigitNumberToPassiAndReturnsItToTheLoginPage()
        {
            var result = await Login(Controller());

            var sent = (StartLoginDto)_rest.Requests.Single().Parameters.OfType<BodyParameter>().Single().Value;
            Assert.That(sent.CheckNumber, Is.InRange(10, 99));

            var page = (CheckInputModel)((OkObjectResult)result).Value;
            Assert.That(page.CheckNumber, Is.EqualTo(sent.CheckNumber));
            Assert.That(page.CheckColor, Is.EqualTo(sent.CheckColor.ToString()), "color is still shown for older app versions");
        }

        [Test]
        public async Task LoginCanReturnEveryDocumentedCheckColor()
        {
            var seenColors = new HashSet<string>();
            for (var i = 0; i < 200; i++)
            {
                var result = await Login(Controller());
                var page = (CheckInputModel)((OkObjectResult)result).Value;
                seenColors.Add(page.CheckColor);
            }

            // All four Color enum members (blue, red, green, yellow) must be reachable.
            // With a uniform pick over 4 colors, missing one after 200 tries is
            // astronomically unlikely; if it's missing every time, the picker is broken.
            Assert.That(seenColors, Is.EquivalentTo(new[] { "blue", "red", "green", "yellow" }));
        }

        [Test]
        public async Task LoginNumbersVaryAcrossLogins()
        {
            var numbers = new HashSet<int>();
            for (var i = 0; i < 40; i++)
            {
                var controller = Controller();
                await Login(controller);
                numbers.Add(((StartLoginDto)_rest.Requests.Single().Parameters.OfType<BodyParameter>().Single().Value).CheckNumber!.Value);
            }

            Assert.That(numbers.Count, Is.GreaterThan(5));
        }

        [Test]
        public async Task LoginReturnsBadRequestInsteadOfThrowingWhenPassiResponseBodyIsEmpty()
        {
            // The internal api/auth/start call can come back HTTP-successful with an empty/null body
            // (e.g. a proxy truncating the response). Login must reject that cleanly instead of
            // dereferencing a null LoginResponceDto.
            var rest = new NullContentRestClient();
            var appSetting = new AppSetting(new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string> { ["AppSetting:startRequest"] = "/api/auth/start" })
                .Build()) { PrefferAppsettingFile = true };
            var controller = new ApiController(new FixedRandom(), rest, appSetting, NullLogger<ApiController>.Instance,
                new FixedClientStore(Client()), null, new FixedLoginRateLimiter(true),
                new FixedLoginStartTokenService(true));
            controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };

            var result = await Login(controller);

            var badRequest = (BadRequestObjectResult)result;
            Assert.That(((ApiResponseDto)badRequest.Value).errors, Is.EqualTo("Internal error"));
        }

        [TestCase(null)]
        [TestCase("unknown")]
        public async Task InvalidClientPreventsPassiLogin(string clientId)
        {
            var controller = Controller();

            var result = (BadRequestObjectResult)await Login(controller, clientId: clientId);

            Assert.That(_rest.Requests, Is.Empty);
            Assert.That(Property(result, "error"), Is.EqualTo("invalid_request"));
            Assert.That(Property(result, "redirect_url"), Is.Null);
        }

        [Test]
        public async Task InvalidRedirectUriPreventsPassiLoginWithoutRedirecting()
        {
            var controller = Controller();

            var result = (BadRequestObjectResult)await Login(controller, redirectUri: "https://attacker.example/cb");

            Assert.That(_rest.Requests, Is.Empty);
            Assert.That(Property(result, "error"), Is.EqualTo("invalid_request"));
            Assert.That(Property(result, "redirect_url"), Is.Null);
        }

        [Test]
        public async Task UnsupportedResponseTypePreventsPassiLoginAndReturnsSafeOAuthError()
        {
            var controller = Controller();

            var result = (BadRequestObjectResult)await Login(controller, responseType: "token");

            Assert.That(_rest.Requests, Is.Empty);
            Assert.That(Property(result, "error"), Is.EqualTo("unsupported_response_type"));
            Assert.That(Property(result, "redirect_url"), Is.EqualTo(
                "https://site/cb?error=unsupported_response_type&error_description=response_type%20must%20be%20code&state=state"));
        }

        [Test]
        public async Task UnsupportedScopePreventsPassiLoginAndReturnsSafeOAuthError()
        {
            var controller = Controller();

            var result = (BadRequestObjectResult)await Login(controller, scope: "openid admin");

            Assert.That(_rest.Requests, Is.Empty);
            Assert.That(Property(result, "error"), Is.EqualTo("invalid_scope"));
            Assert.That(Property(result, "redirect_url"), Does.StartWith("https://site/cb?error=invalid_scope"));
        }

        [Test]
        public async Task RequiredPkcePreventsPassiLoginWhenChallengeIsMissing()
        {
            var controller = Controller(Client(requiresPkce: true));

            var result = (BadRequestObjectResult)await Login(controller);

            Assert.That(_rest.Requests, Is.Empty);
            Assert.That(Property(result, "error"), Is.EqualTo("invalid_request"));
            Assert.That(Property(result, "redirect_url"), Does.Contain("PKCE"));
        }

        [Test]
        public async Task AuthorizePreflightValidatesWithoutStartingPassiLogin()
        {
            var controller = Controller();

            var result = await controller.Authorize(
                "SampleApp", "https://site/cb", "code", "openid profile email", "state", "nonce", null, null, null);

            Assert.That(result, Is.TypeOf<OkObjectResult>());
            Assert.That(Property((OkObjectResult)result, "login_token"), Is.EqualTo("login-token"));
            Assert.That(controller.Response.Headers.CacheControl.ToString(), Is.EqualTo("no-store"));
            Assert.That(_rest.Requests, Is.Empty);
        }

        [Test]
        public async Task TokenIssuanceRateLimitPreventsTokenCreation()
        {
            var tokens = new FixedLoginStartTokenService(true);
            var controller = Controller(
                loginRateLimiter: new FixedLoginRateLimiter(true, false),
                loginStartTokenService: tokens);

            var result = (ObjectResult)await controller.Authorize(
                "SampleApp", "https://site/cb", "code", "openid profile email", "state", "nonce", null, null, null);

            Assert.That(result.StatusCode, Is.EqualTo(StatusCodes.Status429TooManyRequests));
            Assert.That(tokens.IssueCount, Is.Zero);
            Assert.That(_rest.Requests, Is.Empty);
        }

        [Test]
        public async Task MissingOrInvalidLoginTokenPreventsPassiLogin()
        {
            var controller = Controller(loginStartTokenService: new FixedLoginStartTokenService(false));

            var result = (BadRequestObjectResult)await Login(controller);

            Assert.That(_rest.Requests, Is.Empty);
            Assert.That(Property(result, "error"), Is.EqualTo("invalid_request"));
        }

        [Test]
        public async Task CrossSiteAuthorizeRequestIsRejectedBeforeTokenIssuance()
        {
            var tokens = new FixedLoginStartTokenService(true);
            var controller = Controller(loginStartTokenService: tokens);
            controller.Request.Headers["Sec-Fetch-Site"] = "cross-site";

            var result = (ObjectResult)await controller.Authorize(
                "SampleApp", "https://site/cb", "code", "openid profile email", "state", "nonce", null, null, null);

            Assert.That(result.StatusCode, Is.EqualTo(StatusCodes.Status403Forbidden));
            Assert.That(tokens.IssueCount, Is.Zero);
        }

        [Test]
        public async Task RepeatedAuthorizationParameterIsRejectedBeforeTokenIssuance()
        {
            var tokens = new FixedLoginStartTokenService(true);
            var controller = Controller(loginStartTokenService: tokens);
            controller.Request.QueryString = new QueryString("?client_id=SampleApp&client_id=other");

            var result = (BadRequestObjectResult)await controller.Authorize(
                "SampleApp", "https://site/cb", "code", "openid profile email", "state", "nonce", null, null, null);

            Assert.That(Property(result, "error"), Is.EqualTo("invalid_request"));
            Assert.That(tokens.IssueCount, Is.Zero);
        }

        [Test]
        public async Task RateLimitPreventsPassiLoginAndReturnsSafeOAuthError()
        {
            var controller = Controller(loginRateLimiter: new FixedLoginRateLimiter(false));

            var result = (ObjectResult)await Login(controller);

            Assert.That(result.StatusCode, Is.EqualTo(StatusCodes.Status429TooManyRequests));
            Assert.That(_rest.Requests, Is.Empty);
            Assert.That(controller.Response.Headers.RetryAfter.ToString(), Is.EqualTo("300"));
            Assert.That(Property(result, "error"), Is.EqualTo("temporarily_unavailable"));
            Assert.That(Property(result, "redirect_url"), Does.StartWith(
                "https://site/cb?error=temporarily_unavailable"));
        }

        [Test]
        public async Task RateLimiterFailurePreventsPassiLogin()
        {
            var controller = Controller(loginRateLimiter: new ThrowingLoginRateLimiter());

            var result = (ObjectResult)await Login(controller);

            Assert.That(result.StatusCode, Is.EqualTo(StatusCodes.Status503ServiceUnavailable));
            Assert.That(_rest.Requests, Is.Empty);
            Assert.That(Property(result, "error"), Is.EqualTo("temporarily_unavailable"));
        }

        private static object Property(ObjectResult result, string name) =>
            result.Value?.GetType().GetProperty(name)?.GetValue(result.Value);

        private class FixedRandom : IRandomGenerator
        {
            public string GetNumbersString(int i) => new string('1', i);
        }

        private class NullContentRestClient : IMyRestClient
        {
            public Task<RestResponse> ExecuteAsync(RestRequest request) => Task.FromResult(new RestResponse(request)
            {
                StatusCode = HttpStatusCode.OK,
                IsSuccessStatusCode = true,
                ResponseStatus = ResponseStatus.Completed,
                Content = "null",
            });
        }

        private class CapturingRestClient : IMyRestClient
        {
            public readonly List<RestRequest> Requests = new();

            public Task<RestResponse> ExecuteAsync(RestRequest request)
            {
                Requests.Add(request);
                return Task.FromResult(new RestResponse(request)
                {
                    StatusCode = HttpStatusCode.OK,
                    IsSuccessStatusCode = true,
                    ResponseStatus = ResponseStatus.Completed,
                    Content = $"{{\"SessionId\":\"{Guid.NewGuid()}\",\"RegisteredDevices\":[]}}",
                });
            }
        }

        private class FixedClientStore : IClientStore
        {
            private readonly OidcClient _client;

            public FixedClientStore(OidcClient client) => _client = client;

            public Task<OidcClient> FindByClientIdAsync(string clientId) =>
                Task.FromResult(_client?.ClientId == clientId ? _client : null);

            public Task<OidcClient> GetClientAsync(string clientId) => FindByClientIdAsync(clientId);
            public Task CreateClientAsync(OidcClient client) => Task.CompletedTask;
            public Task DeleteClientAsync(string clientId) => Task.CompletedTask;
            public Task<bool> ValidateClientAsync(string clientId, string clientSecret) => Task.FromResult(false);
        }

        private class FixedLoginRateLimiter : ILoginRateLimiter
        {
            private readonly bool _allowed;
            private readonly bool _tokenAllowed;

            public FixedLoginRateLimiter(bool allowed, bool tokenAllowed = true)
            {
                _allowed = allowed;
                _tokenAllowed = tokenAllowed;
            }

            public bool TryAcquire(HttpContext context, string username, out TimeSpan retryAfter)
            {
                retryAfter = TimeSpan.FromMinutes(5);
                return _allowed;
            }

            public bool TryAcquireToken(HttpContext context, out TimeSpan retryAfter)
            {
                retryAfter = TimeSpan.FromMinutes(5);
                return _tokenAllowed;
            }
        }

        private class ThrowingLoginRateLimiter : ILoginRateLimiter
        {
            public bool TryAcquire(HttpContext context, string username, out TimeSpan retryAfter) =>
                throw new InvalidOperationException("Redis unavailable");

            public bool TryAcquireToken(HttpContext context, out TimeSpan retryAfter) =>
                throw new InvalidOperationException("Redis unavailable");
        }

        private sealed class FixedLoginStartTokenService : ILoginStartTokenService
        {
            private readonly bool _valid;

            public FixedLoginStartTokenService(bool valid) => _valid = valid;
            public int IssueCount { get; private set; }
            public string Issue(HttpContext context, LoginStartTokenRequest request)
            {
                IssueCount++;
                return "login-token";
            }
            public bool TryConsume(HttpContext context, string token, LoginStartTokenRequest request) =>
                _valid && token == "login-token";
            public void BindSession(HttpContext context, string sessionId, LoginStartTokenRequest request) { }
            public bool IsSessionBound(HttpContext context, string sessionId, LoginStartTokenRequest request) => _valid;
            public bool TryConsumeSession(HttpContext context, string sessionId, LoginStartTokenRequest request) => _valid;
        }
    }
}
