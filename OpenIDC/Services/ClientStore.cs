using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OpenIDC.Models;
using RedisClient;

namespace OpenIDC.Services
{
    public interface IClientStore
    {
        Task<OidcClient> FindByClientIdAsync(string clientId);
        Task CreateClientAsync(OidcClient client);
        Task DeleteClientAsync(string clientId);
        Task<bool> ValidateClientAsync(string clientId, string clientSecret);
    }

    public class ClientStore : IClientStore
    {
        private readonly IRedisService _redisService;
        private const string ClientPrefix = "oidc:client:";

        public ClientStore(IRedisService redisService)
        {
            _redisService = redisService;
        }

        public Task<OidcClient> FindByClientIdAsync(string clientId)
        {
            var client = _redisService.Get<OidcClient>($"{ClientPrefix}{clientId}");
            return Task.FromResult(client);
        }

        public Task CreateClientAsync(OidcClient client)
        {
            _redisService.Add($"{ClientPrefix}{client.ClientId}", client, TimeSpan.FromDays(365));
            return Task.CompletedTask;
        }

        public Task DeleteClientAsync(string clientId)
        {
            _redisService.Delete<OidcClient>($"{ClientPrefix}{clientId}");
            return Task.CompletedTask;
        }

        public async Task<bool> ValidateClientAsync(string clientId, string clientSecret)
        {
            var client = await FindByClientIdAsync(clientId);
            return client != null && client.ClientSecret == clientSecret;
        }
    }
}
