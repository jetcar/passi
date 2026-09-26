using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OpenIDC.Models;

namespace OpenIDC.Services
{
    public class ClientRegistrationRequest
    {
        public string DisplayName { get; set; }
        public string ClientType { get; set; }
        public List<string> RedirectUris { get; set; }
    }

    public class ClientUpdateRequest
    {
        public string DisplayName { get; set; }
        public List<string> RedirectUris { get; set; }
    }

    public class RegisteredClientDto
    {
        public string ClientId { get; set; }
        public string DisplayName { get; set; }
        public string ClientType { get; set; }
        public List<string> RedirectUris { get; set; }
        public DateTime CreatedAt { get; set; }

        /// <summary>Only returned on create / secret rotation for web clients; never stored in plaintext.</summary>
        public string ClientSecret { get; set; }
    }

    public class ClientRegistrationException : Exception
    {
        public ClientRegistrationException(string message) : base(message) { }
    }

    public interface IRegisteredClientService
    {
        Task<List<RegisteredClientDto>> ListAsync(string ownerSubject);
        Task<RegisteredClientDto> CreateAsync(string ownerSubject, ClientRegistrationRequest request);
        Task<RegisteredClientDto> UpdateAsync(string ownerSubject, string clientId, ClientUpdateRequest request);
        Task<RegisteredClientDto> RotateSecretAsync(string ownerSubject, string clientId);
        Task<bool> DeleteAsync(string ownerSubject, string clientId);
    }

    public class RegisteredClientService : IRegisteredClientService
    {
        public const int MaxClientsPerOwner = 20;
        public const int MaxRedirectUris = 10;
        public const int MaxDisplayNameLength = 100;
        public const int MaxRedirectUriLength = 2048;

        private readonly IRegisteredClientRepository _repository;

        public RegisteredClientService(IRegisteredClientRepository repository)
        {
            _repository = repository;
        }

        public async Task<List<RegisteredClientDto>> ListAsync(string ownerSubject)
        {
            var clients = await _repository.ListByOwnerAsync(RequireOwner(ownerSubject));
            return clients.Select(x => ToDto(x, null)).ToList();
        }

        public async Task<RegisteredClientDto> CreateAsync(string ownerSubject, ClientRegistrationRequest request)
        {
            ownerSubject = RequireOwner(ownerSubject);
            if (request == null)
                throw new ClientRegistrationException("Request body is required");

            var clientType = request.ClientType?.Trim().ToLowerInvariant();
            if (clientType != RegisteredClientTypes.Web && clientType != RegisteredClientTypes.App)
                throw new ClientRegistrationException("Client type must be 'web' or 'app'");

            var displayName = ValidateDisplayName(request.DisplayName);
            var redirectUris = ValidateRedirectUris(request.RedirectUris);

            if (await _repository.CountByOwnerAsync(ownerSubject) >= MaxClientsPerOwner)
                throw new ClientRegistrationException($"You can register at most {MaxClientsPerOwner} applications");

            string secret = clientType == RegisteredClientTypes.Web ? ClientSecretHasher.GenerateSecret() : null;
            var now = DateTime.UtcNow;
            var client = new RegisteredClient
            {
                Id = Guid.NewGuid(),
                ClientId = ClientSecretHasher.GenerateClientId(),
                ClientSecretHash = secret == null ? null : ClientSecretHasher.Hash(secret),
                DisplayName = displayName,
                ClientType = clientType,
                RedirectUris = redirectUris,
                OwnerSubject = ownerSubject,
                CreatedAt = now,
                UpdatedAt = now,
            };

            await _repository.AddAsync(client);
            return ToDto(client, secret);
        }

        public async Task<RegisteredClientDto> UpdateAsync(string ownerSubject, string clientId, ClientUpdateRequest request)
        {
            if (request == null)
                throw new ClientRegistrationException("Request body is required");

            var client = await FindOwnedAsync(ownerSubject, clientId);
            if (client == null)
                return null;

            client.DisplayName = ValidateDisplayName(request.DisplayName);
            client.RedirectUris = ValidateRedirectUris(request.RedirectUris);
            client.UpdatedAt = DateTime.UtcNow;
            await _repository.UpdateAsync(client);
            return ToDto(client, null);
        }

        public async Task<RegisteredClientDto> RotateSecretAsync(string ownerSubject, string clientId)
        {
            var client = await FindOwnedAsync(ownerSubject, clientId);
            if (client == null)
                return null;
            if (client.ClientType != RegisteredClientTypes.Web)
                throw new ClientRegistrationException("Only website clients have a secret");

            var secret = ClientSecretHasher.GenerateSecret();
            client.ClientSecretHash = ClientSecretHasher.Hash(secret);
            client.UpdatedAt = DateTime.UtcNow;
            await _repository.UpdateAsync(client);
            return ToDto(client, secret);
        }

        public async Task<bool> DeleteAsync(string ownerSubject, string clientId)
        {
            var client = await FindOwnedAsync(ownerSubject, clientId);
            if (client == null)
                return false;

            await _repository.DeleteAsync(client);
            return true;
        }

        /// <summary>Returns null when the client doesn't exist or belongs to someone else (no existence leak).</summary>
        private async Task<RegisteredClient> FindOwnedAsync(string ownerSubject, string clientId)
        {
            ownerSubject = RequireOwner(ownerSubject);
            if (string.IsNullOrWhiteSpace(clientId))
                return null;

            var client = await _repository.FindByClientIdAsync(clientId);
            return client != null && client.OwnerSubject == ownerSubject ? client : null;
        }

        private static string RequireOwner(string ownerSubject)
        {
            if (string.IsNullOrWhiteSpace(ownerSubject))
                throw new UnauthorizedAccessException("Missing user identity");
            return ownerSubject;
        }

        private static string ValidateDisplayName(string displayName)
        {
            displayName = displayName?.Trim();
            if (string.IsNullOrEmpty(displayName))
                throw new ClientRegistrationException("Name is required");
            if (displayName.Length > MaxDisplayNameLength)
                throw new ClientRegistrationException($"Name must be at most {MaxDisplayNameLength} characters");
            return displayName;
        }

        public static List<string> ValidateRedirectUris(IEnumerable<string> redirectUris)
        {
            var uris = (redirectUris ?? Enumerable.Empty<string>())
                .Select(x => x?.Trim())
                .Where(x => !string.IsNullOrEmpty(x))
                .Distinct(StringComparer.Ordinal)
                .ToList();

            if (uris.Count == 0)
                throw new ClientRegistrationException("At least one redirect URI is required");
            if (uris.Count > MaxRedirectUris)
                throw new ClientRegistrationException($"At most {MaxRedirectUris} redirect URIs are allowed");

            foreach (var uri in uris)
            {
                if (uri.Length > MaxRedirectUriLength)
                    throw new ClientRegistrationException("Redirect URI is too long");
                if (!Uri.TryCreate(uri, UriKind.Absolute, out var parsed))
                    throw new ClientRegistrationException($"Redirect URI is not an absolute URL: {uri}");
                if (!string.IsNullOrEmpty(parsed.Fragment))
                    throw new ClientRegistrationException($"Redirect URI must not contain a fragment: {uri}");

                var isHttps = parsed.Scheme == Uri.UriSchemeHttps;
                var isLoopbackHttp = parsed.Scheme == Uri.UriSchemeHttp && parsed.IsLoopback;
                if (!isHttps && !isLoopbackHttp)
                    throw new ClientRegistrationException($"Redirect URI must use https (http is allowed only for localhost): {uri}");
            }

            return uris;
        }

        private static RegisteredClientDto ToDto(RegisteredClient client, string plaintextSecret) => new RegisteredClientDto
        {
            ClientId = client.ClientId,
            DisplayName = client.DisplayName,
            ClientType = client.ClientType,
            RedirectUris = client.RedirectUris,
            CreatedAt = client.CreatedAt,
            ClientSecret = plaintextSecret,
        };
    }
}
