using System;
using System.Threading.Tasks;
using OpenIDC.Models;
using RedisClient;

namespace OpenIDC.Services
{
    public interface IRefreshTokenStore
    {
        Task<string> CreateRefreshTokenAsync(RefreshToken refreshToken);
        Task<RefreshToken> ConsumeRefreshTokenAsync(string token);
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

        public Task<RefreshToken> ConsumeRefreshTokenAsync(string token)
        {
            // Atomically read-and-delete: two concurrent token-refresh requests presenting the same
            // refresh token must never both succeed in redeeming it (see
            // RefreshTokenStoreConcurrencyTests.ConcurrentTokenRefreshesCannotBothConsumeTheSameRefreshToken).
            var refreshToken = _redisService.GetAndDelete<RefreshToken>($"{TokenPrefix}{token}");
            return Task.FromResult(refreshToken);
        }
    }
}
