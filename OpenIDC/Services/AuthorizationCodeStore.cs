using System;
using System.Threading.Tasks;
using OpenIDC.Models;
using RedisClient;

namespace OpenIDC.Services
{
    public interface IAuthorizationCodeStore
    {
        Task<string> CreateAuthorizationCodeAsync(AuthorizationCode authCode);
        Task StoreAuthorizationCodeAsync(AuthorizationCode authCode);
        Task<AuthorizationCode> ConsumeAuthorizationCodeAsync(string code);
    }

    public class AuthorizationCodeStore : IAuthorizationCodeStore
    {
        private readonly IRedisService _redisService;
        private const string CodePrefix = "oidc:authcode:";

        public AuthorizationCodeStore(IRedisService redisService)
        {
            _redisService = redisService;
        }

        public Task<string> CreateAuthorizationCodeAsync(AuthorizationCode authCode)
        {
            var code = Guid.NewGuid().ToString("N");
            authCode.Code = code;
            authCode.ExpiresAt = DateTime.UtcNow.AddMinutes(5);

            _redisService.Add($"{CodePrefix}{code}", authCode, TimeSpan.FromMinutes(5));
            return Task.FromResult(code);
        }

        public Task<AuthorizationCode> ConsumeAuthorizationCodeAsync(string code)
        {
            // Atomically read-and-delete: two concurrent token-exchange requests presenting the same
            // authorization code must never both succeed in redeeming it (see
            // AuthorizationCodeStoreConcurrencyTests.ConcurrentTokenExchangesCannotBothConsumeTheSameAuthorizationCode).
            var authCode = _redisService.GetAndDelete<AuthorizationCode>($"{CodePrefix}{code}");
            return Task.FromResult(authCode);
        }

        public Task StoreAuthorizationCodeAsync(AuthorizationCode authCode)
        {
            if (string.IsNullOrEmpty(authCode.Code))
            {
                throw new ArgumentException("Authorization code must have a Code value", nameof(authCode));
            }

            if (authCode.ExpiresAt == default)
            {
                authCode.ExpiresAt = DateTime.UtcNow.AddMinutes(5);
            }

            _redisService.Add($"{CodePrefix}{authCode.Code}", authCode, TimeSpan.FromMinutes(5));
            return Task.CompletedTask;
        }
    }
}
