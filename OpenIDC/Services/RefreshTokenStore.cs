using System;
using System.Threading.Tasks;
using OpenIDC.Models;
using RedisClient;

namespace OpenIDC.Services
{
    public interface IRefreshTokenStore
    {
        Task<string> CreateRefreshTokenAsync(RefreshToken refreshToken);
        Task<RefreshToken> GetRefreshTokenAsync(string token);
        Task RevokeRefreshTokenAsync(string token);
    }

    public class RefreshTokenStore : IRefreshTokenStore
    {
        private readonly IRedisService _redisService;
        private const string TokenPrefix = "oidc:refresh:";

        public RefreshTokenStore(IRedisService redisService)
        {
            _redisService = redisService;
        }

        public Task<string> CreateRefreshTokenAsync(RefreshToken refreshToken)
        {
            var token = Guid.NewGuid().ToString("N");
            refreshToken.Token = token;
            refreshToken.ExpiresAt = DateTime.UtcNow.AddDays(30);

            _redisService.Add($"{TokenPrefix}{token}", refreshToken, TimeSpan.FromDays(30));
            return Task.FromResult(token);
        }

        public Task<RefreshToken> GetRefreshTokenAsync(string token)
        {
            var refreshToken = _redisService.Get<RefreshToken>($"{TokenPrefix}{token}");
            return Task.FromResult(refreshToken);
        }

        public Task RevokeRefreshTokenAsync(string token)
        {
            _redisService.Delete<RefreshToken>($"{TokenPrefix}{token}");
            return Task.CompletedTask;
        }
    }
}
