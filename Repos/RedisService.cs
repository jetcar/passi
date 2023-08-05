using StackExchange.Redis;
using System;
using ConfigurationManager;
using Models;
using Newtonsoft.Json;
using PostSharp.Extensibility;

namespace Repos
{
[ReposProfile(AttributeTargetElements = MulticastTargets.Method)]
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

        public void Add(SessionTempRecord sessionDb)
        {
            var expiry = sessionDb.ExpirationTime - DateTime.UtcNow;
            _database.StringSet(new RedisKey(sessionDb.Guid.ToString()), new RedisValue(JsonConvert.SerializeObject(sessionDb)), expiry);
        }

        public SessionTempRecord Get(Guid guid)
        {
            var redisValue = _database.StringGet(new RedisKey(guid.ToString()));
            if (redisValue.HasValue)
            {
                var value = redisValue.ToString();
                return JsonConvert.DeserializeObject<SessionTempRecord>(value);
            }

            return null;
        }

        public void Delete(Guid guid)
        {
            _database.KeyDelete(new RedisKey(guid.ToString()));
        }
    }

    public interface IRedisService
    {
        void Add(SessionTempRecord sessionDb);
        SessionTempRecord Get(Guid guid);
        void Delete(Guid guid);
    }
}
