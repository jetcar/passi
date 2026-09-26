using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using OpenIDC.Models;
using OpenIDC.Services;

namespace OpenIDCTests
{
    public class FakeRegisteredClientRepository : IRegisteredClientRepository
    {
        public readonly List<RegisteredClient> Clients = new();

        public Task<RegisteredClient> FindByClientIdAsync(string clientId) =>
            Task.FromResult(Clients.FirstOrDefault(x => x.ClientId == clientId));

        public Task<List<RegisteredClient>> ListByOwnerAsync(string ownerSubject) =>
            Task.FromResult(Clients.Where(x => x.OwnerSubject == ownerSubject).ToList());

        public Task<int> CountByOwnerAsync(string ownerSubject) =>
            Task.FromResult(Clients.Count(x => x.OwnerSubject == ownerSubject));

        public Task AddAsync(RegisteredClient client) { Clients.Add(client); return Task.CompletedTask; }

        public Task UpdateAsync(RegisteredClient client)
        {
            Clients.RemoveAll(x => x.Id == client.Id);
            Clients.Add(client);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(RegisteredClient client) { Clients.RemoveAll(x => x.Id == client.Id); return Task.CompletedTask; }
    }

    public class RegisteredClientServiceTests
    {
        private FakeRegisteredClientRepository _repo;
        private RegisteredClientService _service;

        [SetUp]
        public void SetUp()
        {
            _repo = new FakeRegisteredClientRepository();
            _service = new RegisteredClientService(_repo);
        }

        private static ClientRegistrationRequest Web(params string[] uris) =>
            new() { DisplayName = "My site", ClientType = "web", RedirectUris = uris.ToList() };

        [Test]
        public async Task CreateWebClientReturnsSecretOnceAndStoresOnlyItsHash()
        {
            var created = await _service.CreateAsync("alice", Web("https://example.com/cb"));

            Assert.That(created.ClientId, Does.StartWith("pc_"));
            Assert.That(created.ClientSecret, Is.Not.Empty);
            var stored = _repo.Clients.Single();
            Assert.That(stored.OwnerSubject, Is.EqualTo("alice"));
            Assert.That(stored.ClientSecretHash, Is.EqualTo(ClientSecretHasher.Hash(created.ClientSecret)));
            Assert.That(stored.ClientSecretHash, Is.Not.EqualTo(created.ClientSecret));
        }

        [Test]
        public async Task CreateAppClientHasNoSecret()
        {
            var created = await _service.CreateAsync("alice",
                new ClientRegistrationRequest { DisplayName = "App", ClientType = "app", RedirectUris = new() { "http://localhost:8080/cb" } });

            Assert.That(created.ClientSecret, Is.Null);
            Assert.That(_repo.Clients.Single().ClientSecretHash, Is.Null);
        }

        [Test]
        public async Task ListNeverReturnsSecrets()
        {
            await _service.CreateAsync("alice", Web("https://example.com/cb"));

            var list = await _service.ListAsync("alice");

            Assert.That(list.Single().ClientSecret, Is.Null);
        }

        [TestCase("http://example.com/cb")]
        [TestCase("https://example.com/cb#frag")]
        [TestCase("/relative/cb")]
        [TestCase("javascript:alert(1)")]
        public void CreateRejectsUnsafeRedirectUris(string uri)
        {
            Assert.ThrowsAsync<ClientRegistrationException>(() => _service.CreateAsync("alice", Web(uri)));
        }

        [TestCase("https://example.com/cb")]
        [TestCase("http://localhost:5000/cb")]
        [TestCase("http://127.0.0.1/cb")]
        public void RedirectUriValidationAcceptsHttpsAndLoopbackHttp(string uri)
        {
            Assert.That(RegisteredClientService.ValidateRedirectUris(new[] { uri }), Is.EqualTo(new[] { uri }));
        }

        [Test]
        public void CreateRejectsMissingRedirectUrisAndUnknownType()
        {
            Assert.ThrowsAsync<ClientRegistrationException>(() => _service.CreateAsync("alice", Web()));
            Assert.ThrowsAsync<ClientRegistrationException>(() => _service.CreateAsync("alice",
                new ClientRegistrationRequest { DisplayName = "x", ClientType = "admin", RedirectUris = new() { "https://a.com" } }));
        }

        [Test]
        public async Task CreateEnforcesPerOwnerLimit()
        {
            for (var i = 0; i < RegisteredClientService.MaxClientsPerOwner; i++)
                await _service.CreateAsync("alice", Web("https://example.com/cb"));

            Assert.ThrowsAsync<ClientRegistrationException>(() => _service.CreateAsync("alice", Web("https://example.com/cb")));
            Assert.DoesNotThrowAsync(() => _service.CreateAsync("bob", Web("https://example.com/cb")));
        }

        [Test]
        public async Task OtherUsersCannotUpdateRotateOrDeleteAClient()
        {
            var created = await _service.CreateAsync("alice", Web("https://example.com/cb"));

            Assert.That(await _service.UpdateAsync("mallory", created.ClientId,
                new ClientUpdateRequest { DisplayName = "pwned", RedirectUris = new() { "https://evil.com/cb" } }), Is.Null);
            Assert.That(await _service.RotateSecretAsync("mallory", created.ClientId), Is.Null);
            Assert.That(await _service.DeleteAsync("mallory", created.ClientId), Is.False);

            var stored = _repo.Clients.Single();
            Assert.That(stored.DisplayName, Is.EqualTo("My site"));
            Assert.That(stored.RedirectUris, Is.EqualTo(new[] { "https://example.com/cb" }));
        }

        [Test]
        public async Task RotateSecretInvalidatesOldSecret()
        {
            var created = await _service.CreateAsync("alice", Web("https://example.com/cb"));

            var rotated = await _service.RotateSecretAsync("alice", created.ClientId);

            var hash = _repo.Clients.Single().ClientSecretHash;
            Assert.That(ClientSecretHasher.Verify(rotated.ClientSecret, hash), Is.True);
            Assert.That(ClientSecretHasher.Verify(created.ClientSecret, hash), Is.False);
        }

        [Test]
        public void MissingOwnerIsRejected()
        {
            Assert.ThrowsAsync<UnauthorizedAccessException>(() => _service.CreateAsync(null, Web("https://example.com/cb")));
        }
    }

    public class ClientStoreRegisteredClientTests
    {
        [Test]
        public async Task RegisteredWebClientIsResolvedAndValidatedByHash()
        {
            var repo = new FakeRegisteredClientRepository();
            var created = await new RegisteredClientService(repo).CreateAsync("alice",
                new ClientRegistrationRequest { DisplayName = "Site", ClientType = "web", RedirectUris = new() { "https://example.com/cb" } });
            var store = new ClientStore(new ClientStoreTests.FakeRedisService(), repo);

            var client = await store.FindByClientIdAsync(created.ClientId);

            Assert.That(client.RedirectUris, Is.EqualTo(new[] { "https://example.com/cb" }));
            Assert.That(client.RequireClientSecret, Is.True);
            Assert.That(client.ClientSecret, Is.Null);
            Assert.That(await store.ValidateClientAsync(created.ClientId, created.ClientSecret), Is.True);
            Assert.That(await store.ValidateClientAsync(created.ClientId, "wrong"), Is.False);
            Assert.That(await store.ValidateClientAsync(created.ClientId, null), Is.False);
        }

        [Test]
        public async Task RegisteredAppClientIsPublicAndRequiresPkce()
        {
            var repo = new FakeRegisteredClientRepository();
            var created = await new RegisteredClientService(repo).CreateAsync("alice",
                new ClientRegistrationRequest { DisplayName = "App", ClientType = "app", RedirectUris = new() { "http://localhost/cb" } });
            var store = new ClientStore(new ClientStoreTests.FakeRedisService(), repo);

            var client = await store.FindByClientIdAsync(created.ClientId);

            Assert.That(client.IsPublicClient, Is.True);
            Assert.That(client.RequiresPkce, Is.True);
            Assert.That(client.RequireClientSecret, Is.False);
        }

        [Test]
        public async Task BuiltInRedisClientTakesPrecedence()
        {
            var repo = new FakeRegisteredClientRepository();
            var redis = new ClientStoreTests.FakeRedisService();
            var store = new ClientStore(redis, repo);
            await store.CreateClientAsync(new OidcClient { ClientId = "SampleApp", ClientSecret = "s" });

            var client = await store.FindByClientIdAsync("SampleApp");

            Assert.That(client.ClientSecret, Is.EqualTo("s"));
            Assert.That(await store.FindByClientIdAsync("unknown"), Is.Null);
        }
    }
}
