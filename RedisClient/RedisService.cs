using System;
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
    }

    public interface IRedisService
    {
        public void Add<T>(string key, T item, TimeSpan expire);

        void Add<T>(string key, T item);

        T Get<T>(string key);

        void Delete<T>(string key);
    }
}