using System.Text;
using ConfigurationManager;
using CSRedis;
using GoogleTracer;
using Newtonsoft.Json;

namespace RedisClient
{
    [Profile]
    public class RedisService : IRedisService
    {
        private AppSetting _appSetting;
        private readonly CSRedisClient _redis;

        public RedisService(AppSetting appSetting)
        {
            _appSetting = appSetting;
            _redis = new CSRedis.CSRedisClient($"{_appSetting["redis"]}:{_appSetting["redisPort"]}"); ;
        }

        public void Add<T>(string key, T item, TimeSpan expire)
        {
            var json = JsonConvert.SerializeObject(item);
            var newKey = typeof(T).FullName + "." + key;
            _redis.Set(newKey, json, expire);
        }

        public void Add<T>(string key, T item)
        {
            var json = JsonConvert.SerializeObject(item);
            var newKey = typeof(T).FullName + "." + key;
            _redis.Set(newKey, json, new TimeSpan(0, 5, 0));
        }

        public T Get<T>(string key)
        {
            var newKey = typeof(T).FullName + "." + key;

            var redisValue = _redis.Get(newKey);
            if (redisValue != null)
            {
                return JsonConvert.DeserializeObject<T>(redisValue);
            }

            return default(T);
        }

        public void Delete<T>(string key)
        {
            var newKey = typeof(T).FullName + "." + key;
            _redis.Del(newKey);
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