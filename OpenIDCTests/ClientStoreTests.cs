using System;
using System.Collections.Generic;
using NUnit.Framework;
using OpenIDC.Models;
using OpenIDC.Services;
using RedisClient;

namespace OpenIDCTests
{
    public class ClientStoreTests
    {
        [Test]
        public async System.Threading.Tasks.Task ValidateClientAsyncReturnsTrueForCorrectSecret()
        {
            var redis = new FakeRedisService();
            var store = new ClientStore(redis);
            await store.CreateClientAsync(new OidcClient { ClientId = "client1", ClientSecret = "correct-secret" });

            var result = await store.ValidateClientAsync("client1", "correct-secret");

            Assert.That(result, Is.True);
        }

        [Test]
        public async System.Threading.Tasks.Task ValidateClientAsyncReturnsFalseForWrongSecret()
        {
            var redis = new FakeRedisService();
            var store = new ClientStore(redis);
            await store.CreateClientAsync(new OidcClient { ClientId = "client1", ClientSecret = "correct-secret" });

            var result = await store.ValidateClientAsync("client1", "wrong-secret");

            Assert.That(result, Is.False);
        }

        [Test]
        public async System.Threading.Tasks.Task ValidateClientAsyncReturnsFalseForUnknownClient()
        {
            var redis = new FakeRedisService();
            var store = new ClientStore(redis);

            var result = await store.ValidateClientAsync("no-such-client", "whatever");

            Assert.That(result, Is.False);
        }

        [Test]
        public async System.Threading.Tasks.Task ValidateClientAsyncReturnsFalseWhenSuppliedSecretIsNull()
        {
            var redis = new FakeRedisService();
            var store = new ClientStore(redis);
            await store.CreateClientAsync(new OidcClient { ClientId = "client1", ClientSecret = "correct-secret" });

            var result = await store.ValidateClientAsync("client1", null);

            Assert.That(result, Is.False);
        }

        [Test]
        public async System.Threading.Tasks.Task ValidateClientAsyncReturnsFalseWhenStoredSecretIsNull()
        {
            var redis = new FakeRedisService();
            var store = new ClientStore(redis);
            await store.CreateClientAsync(new OidcClient { ClientId = "client1", ClientSecret = null });

            var result = await store.ValidateClientAsync("client1", "anything");

            Assert.That(result, Is.False);
        }

        // Minimal in-memory stand-in for IRedisService so ClientStore can be unit tested
        // without a real Redis instance.
        internal class FakeRedisService : IRedisService
        {
            private readonly Dictionary<string, object> _store = new Dictionary<string, object>();

            public void Add<T>(string key, T item, TimeSpan expire) => _store[key] = item;

            public void Add<T>(string key, T item) => _store[key] = item;

            public T Get<T>(string key) => _store.TryGetValue(key, out var value) ? (T)value : default;

            public void Delete<T>(string key) => _store.Remove(key);
        }
    }
}
