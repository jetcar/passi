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
            refreshToken.FamilyId ??= Guid.NewGuid().ToString("N");
            refreshToken.Token = token;
            refreshToken.ExpiresAt = DateTime.UtcNow.AddDays(30);
            if (refreshToken.ClientExpiresAt < refreshToken.ExpiresAt)
                refreshToken.ExpiresAt = refreshToken.ClientExpiresAt.Value;

            var lifetime = refreshToken.ExpiresAt - DateTime.UtcNow;
            if (lifetime <= TimeSpan.Zero) throw new InvalidOperationException("Client registration has expired.");
            _redisService.Add($"{TokenPrefix}{token}", refreshToken, lifetime);
            // Retain only lineage, not the bearer value, so replay can invalidate the active successor.
            _redisService.Add($"{TokenPrefix}family:{token}", refreshToken.FamilyId, lifetime);
            return Task.FromResult(token);
        }

        public Task<RefreshToken> ConsumeRefreshTokenAsync(string token)
        {
            // Atomically read-and-delete: two concurrent token-refresh requests presenting the same
            // refresh token must never both succeed in redeeming it (see
            // RefreshTokenStoreConcurrencyTests.ConcurrentTokenRefreshesCannotBothConsumeTheSameRefreshToken).
            var refreshToken = _redisService.GetAndDelete<RefreshToken>($"{TokenPrefix}{token}");
            if (refreshToken == null)
            {
                var family = _redisService.Get<string>($"{TokenPrefix}family:{token}");
                if (family != null) _redisService.Add($"{TokenPrefix}revoked:{family}", true, TimeSpan.FromDays(30));
            }
            else if (refreshToken.FamilyId != null && _redisService.Get<bool>($"{TokenPrefix}revoked:{refreshToken.FamilyId}"))
            {
                refreshToken = null;
            }
            return Task.FromResult(refreshToken);
        }
    }
}
