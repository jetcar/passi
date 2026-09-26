using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using OpenIDC.Controllers;
using OpenIDC.Models;
using OpenIDC.Services;

namespace OpenIDCTests
{
    public class AuthorizationControllerLogoutTests
    {
        [TestCase("/")]
        [TestCase("/account/loggedout")]
        public void IsLocalUrlReturnsTrueForLocalPaths(string url)
        {
            Assert.That(AuthorizationController.IsLocalUrl(url), Is.True);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("https://evil.example/phish")]
        [TestCase("//evil.example/phish")]
        [TestCase("/\\evil.example/phish")]
        public void IsLocalUrlReturnsFalseForExternalOrEmptyUrls(string url)
        {
            Assert.That(AuthorizationController.IsLocalUrl(url), Is.False);
        }

        [Test]
        public async Task LogoutPostRedirectsToLocalUri()
        {
            var controller = BuildController(new FakeClientStore(null), "post_logout_redirect_uri=/goodbye");

            var result = await controller.LogoutPost();

            Assert.That(result, Is.InstanceOf<RedirectResult>());
            var redirect = (RedirectResult)result;
            Assert.That(redirect.Url, Is.EqualTo("/goodbye"));
        }

        [Test]
        public async Task LogoutPostRedirectsToRegisteredClientUri()
        {
            var client = new OidcClient
            {
                ClientId = "client1",
                RedirectUris = { "https://client.example/logged-out" }
            };
            var controller = BuildController(new FakeClientStore(client),
                "post_logout_redirect_uri=https%3A%2F%2Fclient.example%2Flogged-out&client_id=client1");

            var result = await controller.LogoutPost();

            Assert.That(result, Is.InstanceOf<RedirectResult>());
            var redirect = (RedirectResult)result;
            Assert.That(redirect.Url, Is.EqualTo("https://client.example/logged-out"));
        }

        [Test]
        public async Task LogoutPostFallsBackToRootForUnregisteredExternalUri()
        {
            var client = new OidcClient
            {
                ClientId = "client1",
                RedirectUris = { "https://client.example/logged-out" }
            };
            var controller = BuildController(new FakeClientStore(client),
                "post_logout_redirect_uri=https%3A%2F%2Fevil.example%2Fphish&client_id=client1");

            var result = await controller.LogoutPost();

            Assert.That(result, Is.InstanceOf<RedirectResult>());
            var redirect = (RedirectResult)result;
            Assert.That(redirect.Url, Is.EqualTo("/"));
        }

        [Test]
        public async Task LogoutPostFallsBackToRootWhenNoClientIdProvided()
        {
            var controller = BuildController(new FakeClientStore(null),
                "post_logout_redirect_uri=https%3A%2F%2Fevil.example%2Fphish");

            var result = await controller.LogoutPost();

            Assert.That(result, Is.InstanceOf<RedirectResult>());
            var redirect = (RedirectResult)result;
            Assert.That(redirect.Url, Is.EqualTo("/"));
        }

        private static AuthorizationController BuildController(IClientStore clientStore, string queryString)
        {
            var controller = new AuthorizationController(clientStore, null, null, null, null);
            var services = new ServiceCollection();
            services.AddSingleton<IAuthenticationService, NoOpAuthenticationService>();
            var httpContext = new DefaultHttpContext
            {
                RequestServices = services.BuildServiceProvider()
            };
            httpContext.Request.QueryString = new QueryString("?" + queryString);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };
            return controller;
        }

        // Stands in for the real authentication service so LogoutPost's
        // HttpContext.SignOutAsync() call has something to resolve.
        private class NoOpAuthenticationService : IAuthenticationService
        {
            public Task<AuthenticateResult> AuthenticateAsync(HttpContext context, string scheme) =>
                Task.FromResult(AuthenticateResult.NoResult());

            public Task ChallengeAsync(HttpContext context, string scheme, AuthenticationProperties properties) =>
                Task.CompletedTask;

            public Task ForbidAsync(HttpContext context, string scheme, AuthenticationProperties properties) =>
                Task.CompletedTask;

            public Task SignInAsync(HttpContext context, string scheme, ClaimsPrincipal principal, AuthenticationProperties properties) =>
                Task.CompletedTask;

            public Task SignOutAsync(HttpContext context, string scheme, AuthenticationProperties properties) =>
                Task.CompletedTask;
        }

        private class FakeClientStore : IClientStore
        {
            private readonly OidcClient _client;

            public FakeClientStore(OidcClient client) => _client = client;

            public Task<OidcClient> FindByClientIdAsync(string clientId) =>
                Task.FromResult(_client != null && _client.ClientId == clientId ? _client : null);

            public Task<OidcClient> GetClientAsync(string clientId) => FindByClientIdAsync(clientId);

            public Task CreateClientAsync(OidcClient client) => Task.CompletedTask;

            public Task DeleteClientAsync(string clientId) => Task.CompletedTask;

            public Task<bool> ValidateClientAsync(string clientId, string clientSecret) => Task.FromResult(false);
        }
    }
}
