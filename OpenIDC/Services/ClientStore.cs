using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using OpenIDC.Models;
using RedisClient;

namespace OpenIDC.Services
{
    public interface IClientStore
    {
        Task<OidcClient> FindByClientIdAsync(string clientId);
        Task<OidcClient> GetClientAsync(string clientId);
        Task CreateClientAsync(OidcClient client);
        Task DeleteClientAsync(string clientId);
        Task<bool> ValidateClientAsync(string clientId, string clientSecret);
    }

    public class ClientStore : IClientStore
    {
        private readonly IRedisService _redisService;
        private readonly IRegisteredClientRepository _registeredClients;
        private const string ClientPrefix = "oidc:client:";

        public ClientStore(IRedisService redisService, IRegisteredClientRepository registeredClients = null)
        {
            _redisService = redisService;
            _registeredClients = registeredClients;
        }

        /// <summary>Built-in (config-seeded) clients live in Redis; user-registered clients in Postgres.</summary>
        public async Task<OidcClient> FindByClientIdAsync(string clientId)
        {
            if (string.IsNullOrEmpty(clientId))
                return null;

            var client = _redisService.Get<OidcClient>($"{ClientPrefix}{clientId}");
            if (client != null || _registeredClients == null)
                return client;

            var registered = await _registeredClients.FindByClientIdAsync(clientId);
            return registered == null ? null : ToOidcClient(registered);
        }

        public static OidcClient ToOidcClient(RegisteredClient registered)
        {
            var isWeb = registered.ClientType == RegisteredClientTypes.Web;
            return new OidcClient
            {
                ClientId = registered.ClientId,
                ClientSecretHash = registered.ClientSecretHash,
                DisplayName = registered.DisplayName,
                RedirectUris = registered.RedirectUris ?? new List<string>(),
                AllowedScopes = new List<string> { "openid", "profile", "email" },
                GrantTypes = new List<string> { "authorization_code", "refresh_token" },
                RequiresPkce = !isWeb,
                RequireClientSecret = isWeb,
                IsPublicClient = !isWeb,
                CreatedAt = registered.CreatedAt,
            };
        }

        public Task<OidcClient> GetClientAsync(string clientId)
        {
            return FindByClientIdAsync(clientId);
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
            if (client == null)
                return false;
            if (!string.IsNullOrEmpty(client.ClientSecretHash))
                return ClientSecretHasher.Verify(clientSecret, client.ClientSecretHash);
            return FixedTimeSecretEquals(client.ClientSecret, clientSecret);
        }

        private static bool FixedTimeSecretEquals(string expected, string actual)
        {
            if (expected == null || actual == null)
                return false;

            return CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(expected),
                Encoding.UTF8.GetBytes(actual));
        }
    }
}
