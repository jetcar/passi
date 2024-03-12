using ConfigurationManager;
using GoogleTracer;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace RedisClient
{
    [Profile]
    public class RedisService : IRedisService
    {
        private readonly ConnectionMultiplexer _redis;
        private readonly IDatabase _database;
        private AppSetting _appSetting;

        public RedisService(AppSetting appSetting)
        {
            _appSetting = appSetting;
            _redis = ConnectionMultiplexer.Connect(_appSetting["redis"]);
            _database = _redis.GetDatabase();
        }

        public void Add<T>(string key, T item, TimeSpan expire)
        {
            var json = JsonConvert.SerializeObject(item);
            var newKey = typeof(T).FullName + "." + key;
            _database.StringSet(newKey, json, expire);
        }

        public void Add<T>(string key, T item)
        {
            var json = JsonConvert.SerializeObject(item);
            var newKey = typeof(T).FullName + "." + key;
            _database.StringSet(newKey, json, new TimeSpan(0, 5, 0));
        }

        public T Get<T>(string key)
        {
            var newKey = typeof(T).FullName + "." + key;
            var redisValue = _database.StringGet(newKey);
            if (redisValue.HasValue)
            {
                var value = redisValue.ToString();
                return JsonConvert.DeserializeObject<T>(value);
            }

            return default(T);
        }

        public void Delete<T>(string key)
        {
            var newKey = typeof(T).FullName + "." + key;
            _database.KeyDelete(new RedisKey(newKey));
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