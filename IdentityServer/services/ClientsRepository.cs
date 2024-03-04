using IdentityModel;
using IdentityRepo.DbContext;
using IdentityServer4.EntityFramework.Entities;
using IdentityServer4.EntityFramework.Mappers;
using Microsoft.EntityFrameworkCore;
using Repos;
using System;
using System.Collections.Generic;
using System.Linq;
using Client = IdentityServer4.EntityFramework.Entities.Client;

namespace IdentityServer.services
{
    public class IdentityClientsRepository : BaseRepo<IdentityDbContext>, IIdentityClientsRepository
    {
        public IdentityClientsRepository(IdentityDbContext dbContext) : base(dbContext)
        {
        }

        public List<Client> GetUserRegisteredClients(string userId)
        {
            List<Client> list = _dbContext.UserClients.Where(x => x.UserId == userId).Select(x => x.Client).ToList();
            return list;
        }

        public Client GetUserRegisteredClientsById(int id, string userId)
        {
            var queryable = _dbContext.UserClients.Include(x => x.Client.RedirectUris).Where(x => x.UserId.ToLower() == userId.ToLower() && x.ClientId == id).Select(x => x.Client);
            return queryable.FirstOrDefault();
        }

        public Client AddClient(IdentityServer4.Models.Client client, string userId)
        {
            Client savedClientEntity = null;
            var strategy = GetExecutionStrategy();
            strategy.Execute(() =>
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
                    savedClientEntity = savedClient.Entity;
                }
            });
            return savedClientEntity;
        }

        public bool IsClientIdTaken(string clientId)
        {
            var isClientIdAvailable = _dbContext.Clients.Any(x => x.ClientId == clientId);
            return isClientIdAvailable;
        }

        public bool Exists(int id, string userId)
        {
            return _dbContext.UserClients.Any(x => x.ClientId == id && x.UserId == userId.ToLower());
        }

        public void Update(int id, string userId, string clientSecret, string returnUrls, string url)
        {
            var strategy = GetExecutionStrategy();
            strategy.Execute(() =>
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
            });
        }

        public List<Client> GetAllClients()
        {
            return _dbContext.Clients.ToList();
        }
    }

    public interface IIdentityClientsRepository : ITransaction
    {
        List<Client> GetUserRegisteredClients(string userId);

        Client GetUserRegisteredClientsById(int id, string userId);

        Client AddClient(IdentityServer4.Models.Client client, string userId);

        bool IsClientIdTaken(string clientId);

        bool Exists(int id, string userId);

        void Update(int id, string userId, string clientSecret, string returnUrls, string url);

        List<Client> GetAllClients();
    }
}