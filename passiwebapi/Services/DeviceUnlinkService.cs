using ConfigurationManager;
using RedisClient;
using Repos;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace passi_webapi.DeviceSecurity
{
    public class DeviceUnlinkGrant
    {
        public long UserId { get; set; }
        public long DeviceId { get; set; }
        public long? UserDeviceId { get; set; }
        public DateTimeOffset ExpiresAt { get; set; }
    }

    public interface IDeviceUnlinkService
    {
        string ApiBaseUrl { get; }
        string Issue(long userId, long deviceId, long? userDeviceId);
        bool TryUnlink(string token);
    }

    public class DeviceUnlinkService : IDeviceUnlinkService
    {
        private static readonly TimeSpan GrantLifetime = TimeSpan.FromMinutes(15);
        private readonly IRedisService _redisService;
        private readonly IUserRepository _userRepository;

        public DeviceUnlinkService(
            IRedisService redisService,
            IUserRepository userRepository,
            AppSetting appSetting)
        {
            _redisService = redisService;
            _userRepository = userRepository;
            ApiBaseUrl = (appSetting["NotificationApiBaseUrl"] ?? appSetting["PassiUrl"])?.TrimEnd('/');
        }

        public string ApiBaseUrl { get; }

        public string Issue(long userId, long deviceId, long? userDeviceId)
        {
            var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
            var expiresAt = DateTimeOffset.UtcNow.Add(GrantLifetime);
            _redisService.Add(
                GetRedisKey(token),
                new DeviceUnlinkGrant
                {
                    UserId = userId,
                    DeviceId = deviceId,
                    UserDeviceId = userDeviceId,
                    ExpiresAt = expiresAt,
                },
                GrantLifetime);
            return token;
        }

        public bool TryUnlink(string token)
        {
            if (string.IsNullOrWhiteSpace(token)
                || token.Length != 64
                || token.Any(x => !Uri.IsHexDigit(x)))
            {
                return false;
            }

            var redisKey = GetRedisKey(token);
            var grant = _redisService.GetAndDelete<DeviceUnlinkGrant>(redisKey);
            if (grant == null)
            {
                return false;
            }

            if (grant.ExpiresAt <= DateTimeOffset.UtcNow)
            {
                return false;
            }

            try
            {
                var strategy = _userRepository.GetExecutionStrategy();
                strategy.Execute(() =>
                {
                    using var transaction = _userRepository.BeginTransaction();
                    _userRepository.DeleteDeviceLink(grant.UserId, grant.DeviceId, grant.UserDeviceId);
                    transaction.Commit();
                });
                return true;
            }
            catch
            {
                var remaining = grant.ExpiresAt - DateTimeOffset.UtcNow;
                if (remaining > TimeSpan.Zero)
                {
                    try
                    {
                        _redisService.Add(redisKey, grant, remaining);
                    }
                    catch
                    {
                        // Best effort: preserving the database failure is more useful than masking it.
                    }
                }
                throw;
            }
        }

        private static string GetRedisKey(string token)
        {
            var tokenHash = SHA256.HashData(Encoding.UTF8.GetBytes(token));
            return Convert.ToHexString(tokenHash);
        }
    }
}
