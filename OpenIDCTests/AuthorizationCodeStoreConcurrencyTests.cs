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
    public class AuthorizationCodeStoreConcurrencyTests
    {
        [Test]
        public async Task ConcurrentTokenExchangesCannotBothConsumeTheSameAuthorizationCode()
        {
            // concurrentReaders: 2 forces both racing calls' reads to rendezvous before either can
            // observe the other's delete - modelling two /connect/token requests whose Redis GETs
            // both land before either GET's caller has had a chance to delete the code.
            var redis = new InMemoryRedisService(concurrentReaders: 2);
            var store = new AuthorizationCodeStore(redis);
            var code = await store.CreateAuthorizationCodeAsync(new AuthorizationCode
            {
                ClientId = "client1",
                Subject = "alice",
                RedirectUri = "https://example.com/cb"
            });

            // Two token-exchange requests racing to redeem the same authorization code, as would
            // happen if an attacker intercepts and replays it alongside the legitimate client.
            var results = await Task.WhenAll(
                Task.Run(() => store.ConsumeAuthorizationCodeAsync(code)),
                Task.Run(() => store.ConsumeAuthorizationCodeAsync(code)));

            Assert.That(results.Count(r => r != null), Is.EqualTo(1),
                "exactly one concurrent request should be able to consume a single-use authorization code; " +
                "both succeeding means the code was replayed and two sets of tokens were minted from one code");
        }

        /// <summary>
        /// Minimal in-memory stand-in for the real Redis-backed IRedisService. When constructed with
        /// concurrentReaders > 1, Get rendezvous with that many other concurrent Get callers before
        /// returning, deterministically reproducing the check-then-act window a naive Get-then-Delete
        /// composition has against a real Redis server (two requests' GETs both land before either
        /// request's DEL has run) instead of relying on an unpredictable real race.
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
