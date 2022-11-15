using System;
using System.Collections.Generic;
using System.Linq;
using IdentityModel;
using IdentityServer.DbContext;
using IdentityServer4.EntityFramework.Entities;
using IdentityServer4.EntityFramework.Mappers;
using IdentityServer4.Models;
using Microsoft.EntityFrameworkCore;
using Client = IdentityServer4.EntityFramework.Entities.Client;

namespace IdentityServer.services
{
    public class IdentityClientsRepository : IIdentityClientsRepository
    {
        private IdentityDbContext _dbContext;

        public IdentityClientsRepository(IdentityDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public List<IdentityServer4.EntityFramework.Entities.Client> GetUserRegisteredClients(string userId)
        {
            using (_dbContext.Database.BeginTransaction())
            {
                return _dbContext.UserClients.Where(x => x.UserId == userId).Select(x => x.Client).ToList();
            }
        }

        public IdentityServer4.EntityFramework.Entities.Client GetUserRegisteredClientsById(int id, string userId)
        {
            using (_dbContext.Database.BeginTransaction())
            {
                var queryable = _dbContext.UserClients.Include(x => x.Client.RedirectUris).Where(x => x.UserId.ToLower() == userId.ToLower() && x.ClientId == id).Select(x => x.Client);
                return queryable.FirstOrDefault();
            }
        }

        public Client AddClient(IdentityServer4.Models.Client client, string userId)
        {
            using (var transaction = _dbContext.Database.BeginTransaction())
            {
                var savedClient = _dbContext.Clients.Add(client.ToEntity());

                var user = new UserClient()
                {
                    UserId = userId.ToLower(),
                    Client = savedClient.Entity
                };
                _dbContext.UserClients.Add(user);
                _dbContext.SaveChanges();
                transaction.Commit();
                return savedClient.Entity;
            }
        }

        public bool IsClientIdTaken(string clientId)
        {
            using (_dbContext.Database.BeginTransaction())
            {
                var isClientIdAvailable = _dbContext.Clients.Any(x => x.ClientId == clientId);
                return isClientIdAvailable;
            }
        }

        public Client GetClientAllDataById(int id, string userId)
        {
            using (_dbContext.Database.BeginTransaction())
            {
                return _dbContext.UserClients
                    .Include(x => x.Client.RedirectUris)
                    .Where(x => x.UserId == userId.ToLower() && x.ClientId == id).Select(x => x.Client)
                    .FirstOrDefault();
            }
        }

        public bool Exists(int id, string userId)
        {
            using (_dbContext.Database.BeginTransaction())
            {
                return _dbContext.UserClients.Any(x => x.ClientId == id && x.UserId == userId.ToLower());
            }
        }

        public void Update(int id, string userId, string clientSecret, string returnUrls, string url)
        {
            using (var transaction = _dbContext.Database.BeginTransaction())
            {
                var client = _dbContext.UserClients
                     .Include(x => x.Client.ClientSecrets)
                     .Include(x => x.Client.RedirectUris)
                     .Where(x => x.UserId == userId.ToLower() && x.ClientId == id).Select(x => x.Client)
                     .FirstOrDefault();
                if (client != null)
                {
                    if (!string.IsNullOrEmpty(clientSecret))
                    {
                        foreach (var secret in client.ClientSecrets)
                        {
                            secret.Expiration = DateTime.Today;
                        }
                        client.ClientSecrets.Add(new ClientSecret() { Value = clientSecret.ToSha256() });
                    }

                    client.ClientUri = url;

                    var urls = returnUrls.Split(',').Select(x => x.Trim());
                    foreach (var returnUrl in urls)
                    {
                        if (client.RedirectUris.Any(x => x.RedirectUri == returnUrl))
                            continue;
                        client.RedirectUris.Add(new ClientRedirectUri() { RedirectUri = returnUrl });
                    }

                    var urlsToRemove = new List<ClientRedirectUri>();
                    foreach (var clientRedirectUri in client.RedirectUris)
                    {
                        if (urls.Contains(clientRedirectUri.RedirectUri))
                            continue;
                        urlsToRemove.Add(clientRedirectUri);
                    }
                    foreach (var clientRedirectUri in urlsToRemove)
                        client.RedirectUris.Remove(clientRedirectUri);

                    _dbContext.SaveChanges();
                    transaction.Commit();
                }
            }
        }
    }

    public interface IIdentityClientsRepository
    {
        List<IdentityServer4.EntityFramework.Entities.Client> GetUserRegisteredClients(string userId);

        IdentityServer4.EntityFramework.Entities.Client GetUserRegisteredClientsById(int id, string userId);

        IdentityServer4.EntityFramework.Entities.Client AddClient(IdentityServer4.Models.Client client, string userId);

        bool IsClientIdTaken(string clientId);

        IdentityServer4.EntityFramework.Entities.Client GetClientAllDataById(int id, string userId);

        bool Exists(int id, string userId);

        void Update(int id, string userId, string clientSecret, string returnUrls, string url);
    }
}