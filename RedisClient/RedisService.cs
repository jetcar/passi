using System;
using System.Collections.Generic;
using System.Globalization;
using ConfigurationManager;
using GoogleTracer;
using Newtonsoft.Json;
using ServiceStack.Redis;

namespace RedisClient
{
    [Profile]
    public class RedisService : IRedisService
    {
        private readonly AppSetting _appSetting;
        private readonly IRedisClientsManager _redisManager;

        public RedisService(AppSetting appSetting)
        {
            _appSetting = appSetting;
            var redisHost = _appSetting["redis"];
            var redisPort = _appSetting["redisPort"];
            var redisPassword = _appSetting["redisPassword"];

            var connectionString = string.IsNullOrEmpty(redisPassword)
                ? $"{redisHost}:{redisPort}"
                : $"{redisPassword}@{redisHost}:{redisPort}";

            _redisManager = new RedisManagerPool(connectionString);
        }

        public void Add<T>(string key, T item, TimeSpan expire)
        {
            using var redis = _redisManager.GetClient();
            var json = JsonConvert.SerializeObject(item);
            var newKey = typeof(T).FullName + "." + key;
            redis.Set(newKey, json, expire);
        }

        public void Add<T>(string key, T item)
        {
            using var redis = _redisManager.GetClient();
            var json = JsonConvert.SerializeObject(item);
            var newKey = typeof(T).FullName + "." + key;
            redis.Set(newKey, json, TimeSpan.FromMinutes(5));
        }

        public T Get<T>(string key)
        {
            using var redis = _redisManager.GetClient();
            var newKey = typeof(T).FullName + "." + key;
            var redisValue = redis.Get<string>(newKey);

            if (redisValue != null)
            {
                return JsonConvert.DeserializeObject<T>(redisValue);
            }

            return default(T);
        }

        public void Delete<T>(string key)
        {
            using var redis = _redisManager.GetClient();
            var newKey = typeof(T).FullName + "." + key;
            redis.Remove(newKey);
        }

        public T GetAndDelete<T>(string key)
        {
            using var redis = _redisManager.GetClient();
            var newKey = typeof(T).FullName + "." + key;

            // Redis executes a client's queued MULTI/EXEC commands as one indivisible batch, so no other
            // client's command can run between the GET and the DEL: two concurrent callers can never both
            // read the value before either one removes it, unlike issuing Get<T> then Delete<T> separately.
            string redisValue = null;
            using (var transaction = redis.CreateTransaction())
            {
                transaction.QueueCommand(r => r.GetValue(newKey), x => redisValue = x);
                transaction.QueueCommand(r => r.Remove(newKey));
                transaction.Commit();
            }

            if (redisValue != null)
            {
                return JsonConvert.DeserializeObject<T>(DecodeStoredString(redisValue));
            }

            return default(T);
        }

        public bool TryAcquireFixedWindow(string key, int permitLimit, TimeSpan window, out TimeSpan retryAfter)
        {
            if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("A rate-limit key is required.", nameof(key));
            if (permitLimit <= 0) throw new ArgumentOutOfRangeException(nameof(permitLimit));
            if (window <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(window));

            // Redis documents this INCR + expiry Lua pattern for race-free fixed-window limiting:
            // https://redis.io/docs/latest/commands/incr/#pattern-rate-limiter
            const string script = @"
local current = redis.call('INCR', KEYS[1])
local ttl = redis.call('PTTL', KEYS[1])
if current == 1 or ttl < 0 then
    redis.call('PEXPIRE', KEYS[1], ARGV[1])
    ttl = tonumber(ARGV[1])
end
return { current, ttl }";

            using var redis = _redisManager.GetClient();
            var result = redis.ExecLuaAsList(
                script,
                new[] { key },
                new[] { ((long)Math.Ceiling(window.TotalMilliseconds)).ToString(CultureInfo.InvariantCulture) });
            if (result == null || result.Count != 2)
                throw new InvalidOperationException("Redis returned an invalid rate-limit response.");

            var current = long.Parse(result[0], CultureInfo.InvariantCulture);
            var ttlMilliseconds = long.Parse(result[1], CultureInfo.InvariantCulture);
            var acquired = current <= permitLimit;
            retryAfter = acquired ? TimeSpan.Zero : TimeSpan.FromMilliseconds(Math.Max(1, ttlMilliseconds));
            return acquired;
        }

        /// <summary>
        /// Add stores through IRedisClient.Set&lt;string&gt;, which ServiceStack JSON-encodes into a quoted
        /// string literal. Get&lt;string&gt; undoes that, but a GET queued in a transaction hands back the raw
        /// bytes, so they must be decoded the same way before Newtonsoft can read the payload.
        /// </summary>
        public static string DecodeStoredString(string rawValue) =>
            ServiceStack.Text.JsonSerializer.DeserializeFromString<string>(rawValue);
    }

    public interface IRedisService
    {
        public void Add<T>(string key, T item, TimeSpan expire);

        void Add<T>(string key, T item);

        T Get<T>(string key);

        void Delete<T>(string key);

        T GetAndDelete<T>(string key);

        bool TryAcquireFixedWindow(string key, int permitLimit, TimeSpan window, out TimeSpan retryAfter)
        {
            throw new NotSupportedException("This Redis service does not support rate limiting.");
        }
    }
}
