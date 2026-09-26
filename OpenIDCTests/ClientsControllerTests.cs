using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using ConfigurationManager;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;
using OpenIDC.Controllers;
using OpenIDC.Services;

namespace OpenIDCTests
{
    public class ClientsControllerTests
    {
        private FakeRegisteredClientRepository _repo;

        [SetUp]
        public void SetUp() => _repo = new FakeRegisteredClientRepository();

        private ClientsController Controller(string sub, string tokenClientId, string allowlist = null)
        {
            var settings = new Dictionary<string, string>();
            if (allowlist != null)
                settings["AppSetting:ClientManagementClientIds"] = allowlist;
            var appSetting = new AppSetting(new ConfigurationBuilder().AddInMemoryCollection(settings).Build())
            {
                PrefferAppsettingFile = true,
            };

            var claims = new List<Claim>();
            if (sub != null) claims.Add(new Claim(ClaimTypes.NameIdentifier, sub));
            if (tokenClientId != null) claims.Add(new Claim("client_id", tokenClientId));

            return new ClientsController(new RegisteredClientService(_repo), appSetting)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext
                    {
                        User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Bearer")),
                    },
                },
            };
        }

        private static ClientRegistrationRequest Web() => new()
        {
            DisplayName = "Site", ClientType = "web", RedirectUris = new() { "https://example.com/cb" },
        };

        [Test]
        public async Task PassiWebsiteTokenCanRegisterClientOwnedByTokenSubject()
        {
            var result = await Controller("alice", "SampleApp").Create(Web());

            Assert.That(result, Is.InstanceOf<OkObjectResult>());
            Assert.That(_repo.Clients.Single().OwnerSubject, Is.EqualTo("alice"));
        }

        [Test]
        public async Task ThirdPartyAppTokenIsForbidden()
        {
            var result = await Controller("alice", "pc_someones_app").Create(Web());

            Assert.That(result, Is.InstanceOf<ForbidResult>());
            Assert.That(_repo.Clients, Is.Empty);
        }

        [Test]
        public async Task TokenWithoutClientIdIsForbidden()
        {
            Assert.That(await Controller("alice", null).List(), Is.InstanceOf<ForbidResult>());
        }

        [Test]
        public async Task AllowlistIsConfigurable()
        {
            Assert.That(await Controller("alice", "SampleApp", "PassiWeb").List(), Is.InstanceOf<ForbidResult>());
            Assert.That(await Controller("alice", "PassiWeb", "PassiWeb, Other").List(), Is.InstanceOf<OkObjectResult>());
        }

        [Test]
        public async Task ListReturnsOnlyCallersClients()
        {
            await Controller("alice", "SampleApp").Create(Web());
            await Controller("bob", "SampleApp").Create(Web());

            var result = (OkObjectResult)await Controller("alice", "SampleApp").List();

            var clients = (List<RegisteredClientDto>)result.Value;
            Assert.That(clients.Count, Is.EqualTo(1));
            Assert.That(_repo.Clients.Single(x => x.ClientId == clients[0].ClientId).OwnerSubject, Is.EqualTo("alice"));
        }

        [Test]
        public async Task DeletingSomeoneElsesClientReturnsNotFound()
        {
            await Controller("alice", "SampleApp").Create(Web());
            var clientId = _repo.Clients.Single().ClientId;

            var result = await Controller("bob", "SampleApp").Delete(clientId);

            Assert.That(result, Is.InstanceOf<NotFoundResult>());
            Assert.That(_repo.Clients, Has.Count.EqualTo(1));
        }

        [Test]
        public async Task InvalidRegistrationReturnsBadRequest()
        {
            var result = await Controller("alice", "SampleApp").Create(new ClientRegistrationRequest
            {
                DisplayName = "Site", ClientType = "web", RedirectUris = new() { "http://example.com/cb" },
            });

            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        }
    }
}
