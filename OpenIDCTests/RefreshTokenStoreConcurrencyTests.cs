using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using OpenIDC.Models;
using OpenIDC.Services;
using RedisClient;

namespace OpenIDCTests
{
    public class RefreshTokenStoreConcurrencyTests
    {
        [Test]
        public async Task ConcurrentTokenRefreshesCannotBothConsumeTheSameRefreshToken()
        {
            // concurrentReaders: 2 forces both racing calls' reads to rendezvous before either can
            // observe the other's delete - modelling two /connect/token refresh requests whose Redis
            // GETs both land before either GET's caller has had a chance to delete the token.
            var redis = new InMemoryRedisService(concurrentReaders: 2);
            var store = new RefreshTokenStore(redis);
            var token = await store.CreateRefreshTokenAsync(new RefreshToken
            {
                ClientId = "client1",
                Subject = "alice"
            });

            // Two refresh requests racing to redeem the same refresh token, as would happen if an
            // attacker who stole a refresh token races the legitimate client's own refresh.
            var results = await Task.WhenAll(
                Task.Run(() => store.ConsumeRefreshTokenAsync(token)),
                Task.Run(() => store.ConsumeRefreshTokenAsync(token)));

            Assert.That(results.Count(r => r != null), Is.EqualTo(1),
                "exactly one concurrent refresh request should be able to consume a single-use refresh token; " +
                "both succeeding means the token was replayed and two sets of tokens were minted from one");
        }

        /// <summary>
        /// Minimal in-memory stand-in for the real Redis-backed IRedisService, matching the one in
        /// AuthorizationCodeStoreConcurrencyTests. When constructed with concurrentReaders > 1, Get
        /// rendezvous with that many other concurrent Get callers before returning, deterministically
        /// reproducing the check-then-act window a naive Get-then-Delete composition has against a
        /// real Redis server.
        /// </summary>
        private class InMemoryRedisService : IRedisService
        {
            private readonly ConcurrentDictionary<string, object> _store = new();
            private readonly Barrier _readBarrier;

            public InMemoryRedisService(int concurrentReaders = 1) => _readBarrier = new Barrier(concurrentReaders);

            public void Add<T>(string key, T item, TimeSpan expire) => _store[Key<T>(key)] = item;

            public void Add<T>(string key, T item) => _store[Key<T>(key)] = item;

            public T Get<T>(string key)
            {
                _readBarrier.SignalAndWait(); // phase 1: wait until every racing reader has arrived
                var found = _store.TryGetValue(Key<T>(key), out var value);
                _readBarrier.SignalAndWait(); // phase 2: wait until every racing reader has also finished reading,
                                               // so none of them can return here and delete before the others read
                return found ? (T)value : default;
            }

            public void Delete<T>(string key) => _store.TryRemove(Key<T>(key), out _);

            public T GetAndDelete<T>(string key) =>
                _store.TryRemove(Key<T>(key), out var value) ? (T)value : default;

            private static string Key<T>(string key) => typeof(T).FullName + "." + key;
        }
    }
}
