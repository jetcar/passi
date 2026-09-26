using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using ConfigurationManager;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using RestSharp;
using Services;
using WebApp.Controllers;

namespace WebAppTests
{
    public class OAuthClientsControllerTests
    {
        private const string OpenIdcUrl = "https://passi.example/openidc";

        private FakeRestClient _rest;

        [SetUp]
        public void SetUp() => _rest = new FakeRestClient();

        private OAuthClientsController Controller(bool authenticated, string accessToken = "user-access-token")
        {
            var appSetting = new AppSetting(new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string> { ["AppSetting:openIdcUrl"] = OpenIdcUrl + "/" })
                .Build()) { PrefferAppsettingFile = true };

            var services = new ServiceCollection()
                .AddSingleton<IAuthenticationService>(new FakeAuthenticationService(accessToken))
                .BuildServiceProvider();

            var identity = authenticated
                ? new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "alice") }, "Cookies")
                : new ClaimsIdentity();

            return new OAuthClientsController(_rest, new FakeAntiforgery(), appSetting)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity), RequestServices = services },
                },
            };
        }

        [Test]
        public async Task AnonymousUserGets401AndNothingIsForwarded()
        {
            Assert.That(await Controller(authenticated: false).List(), Is.InstanceOf<UnauthorizedResult>());
            Assert.That(Controller(authenticated: false).Context(), Is.InstanceOf<UnauthorizedResult>());
            Assert.That(_rest.Requests, Is.Empty);
        }

        [Test]
        public async Task MissingAccessTokenGets401()
        {
            Assert.That(await Controller(authenticated: true, accessToken: null).List(), Is.InstanceOf<UnauthorizedResult>());
            Assert.That(_rest.Requests, Is.Empty);
        }

        [Test]
        public async Task ListForwardsToOpenIdcWithUsersBearerToken()
        {
            _rest.Next = new RestResponse { StatusCode = HttpStatusCode.OK, Content = "[]", ResponseStatus = ResponseStatus.Completed };

            var result = (ContentResult)await Controller(authenticated: true).List();

            var request = _rest.Requests.Single();
            Assert.That(request.Resource, Is.EqualTo(OpenIdcUrl + "/api/clients"));
            Assert.That(request.Method, Is.EqualTo(Method.Get));
            Assert.That(request.Parameters.First(p => p.Name == "Authorization").Value, Is.EqualTo("Bearer user-access-token"));
            Assert.That(result.StatusCode, Is.EqualTo(200));
            Assert.That(result.Content, Is.EqualTo("[]"));
        }

        [Test]
        public async Task CreateForwardsBodyAndPassesThroughErrors()
        {
            _rest.Next = new RestResponse { StatusCode = HttpStatusCode.BadRequest, Content = "{\"errors\":\"bad uri\"}", ResponseStatus = ResponseStatus.Completed };
            var body = JsonDocument.Parse("{\"displayName\":\"Site\",\"clientType\":\"web\",\"redirectUris\":[\"https://a.com/cb\"]}").RootElement;

            var result = (ContentResult)await Controller(authenticated: true).Create(body);

            var request = _rest.Requests.Single();
            Assert.That(request.Method, Is.EqualTo(Method.Post));
            Assert.That(request.Parameters.OfType<BodyParameter>().Single().Value?.ToString(), Does.Contain("\"clientType\":\"web\""));
            Assert.That(result.StatusCode, Is.EqualTo(400));
            Assert.That(result.Content, Does.Contain("bad uri"));
        }

        [Test]
        public async Task ClientIdIsUrlEncodedInForwardedPath()
        {
            _rest.Next = new RestResponse { StatusCode = HttpStatusCode.NoContent, ResponseStatus = ResponseStatus.Completed };

            await Controller(authenticated: true).Delete("pc_a/../b");

            Assert.That(_rest.Requests.Single().Resource, Is.EqualTo(OpenIdcUrl + "/api/clients/pc_a%2F..%2Fb"));
        }

        [Test]
        public async Task UnreachableIdentityServiceReturns502()
        {
            _rest.Next = new RestResponse { StatusCode = 0, ResponseStatus = ResponseStatus.Error };

            var result = await Controller(authenticated: true).List();

            Assert.That(((ObjectResult)result).StatusCode, Is.EqualTo(502));
        }

        private class FakeRestClient : IMyRestClient
        {
            public readonly List<RestRequest> Requests = new();
            public RestResponse Next = new() { StatusCode = HttpStatusCode.OK, Content = "{}", ResponseStatus = ResponseStatus.Completed };

            public Task<RestResponse> ExecuteAsync(RestRequest request)
            {
                Requests.Add(request);
                return Task.FromResult(Next);
            }
        }

        private class FakeAuthenticationService : IAuthenticationService
        {
            private readonly string _accessToken;
            public FakeAuthenticationService(string accessToken) => _accessToken = accessToken;

            public Task<AuthenticateResult> AuthenticateAsync(HttpContext context, string scheme)
            {
                var properties = new AuthenticationProperties();
                if (_accessToken != null)
                    properties.StoreTokens(new[] { new AuthenticationToken { Name = "access_token", Value = _accessToken } });
                return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(context.User, properties, "Cookies")));
            }

            public Task ChallengeAsync(HttpContext context, string scheme, AuthenticationProperties properties) => Task.CompletedTask;
            public Task ForbidAsync(HttpContext context, string scheme, AuthenticationProperties properties) => Task.CompletedTask;
            public Task SignInAsync(HttpContext context, string scheme, ClaimsPrincipal principal, AuthenticationProperties properties) => Task.CompletedTask;
            public Task SignOutAsync(HttpContext context, string scheme, AuthenticationProperties properties) => Task.CompletedTask;
        }

        private class FakeAntiforgery : IAntiforgery
        {
            public AntiforgeryTokenSet GetAndStoreTokens(HttpContext httpContext) => new("req", "cookie", "__RequestVerificationToken", "RequestVerificationToken");
            public AntiforgeryTokenSet GetTokens(HttpContext httpContext) => GetAndStoreTokens(httpContext);
            public Task<bool> IsRequestValidAsync(HttpContext httpContext) => Task.FromResult(true);
            public Task ValidateRequestAsync(HttpContext httpContext) => Task.CompletedTask;
            public void SetCookieTokenAndHeader(HttpContext httpContext) { }
        }
    }
}
