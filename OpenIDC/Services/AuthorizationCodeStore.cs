using System;
using System.Threading.Tasks;
using OpenIDC.Models;
using RedisClient;

namespace OpenIDC.Services
{
    public interface IAuthorizationCodeStore
    {
        Task<string> CreateAuthorizationCodeAsync(AuthorizationCode authCode);
        Task<AuthorizationCode> GetAuthorizationCodeAsync(string code);
        Task RevokeAuthorizationCodeAsync(string code);
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

        public Task<AuthorizationCode> GetAuthorizationCodeAsync(string code)
        {
            var authCode = _redisService.Get<AuthorizationCode>($"{CodePrefix}{code}");
            return Task.FromResult(authCode);
        }

        public Task RevokeAuthorizationCodeAsync(string code)
        {
            _redisService.Delete<AuthorizationCode>($"{CodePrefix}{code}");
            return Task.CompletedTask;
        }
    }
}
