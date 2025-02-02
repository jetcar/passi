using IdentityRepo.DbContext;
using Microsoft.EntityFrameworkCore;
using Repos;
using System;
using System.Collections.Generic;
using System.Linq;
using OpenIddict.Abstractions;
using OpenIddict.Core;

namespace IdentityServer.services
{
    [GoogleTracer.Profile]
    public class IdentityClientsRepository : BaseRepo<IdentityDbContext>, IIdentityClientsRepository
    {
        private OpenIddict.Abstractions.IOpenIddictApplicationManager _manager;
        public IdentityClientsRepository(IdentityDbContext dbContext, IOpenIddictApplicationManager manager) : base(dbContext)
        {
            _manager = manager;
        }

        public List<UserClient> GetUserRegisteredClients(string userId)
        {
            return _dbContext.UserClients.Where(x => x.UserId == userId).ToList();

        }

        public UserClient GetUserRegisteredClientsById(string client_Id, string userId)
        {
            var queryable = _dbContext.UserClients.Where(x => x.UserId.ToLower() == userId.ToLower() && x.ClientId_new == client_Id);
            return queryable.FirstOrDefault();
        }

        public UserClient AddClient(UserClient client, string userId)
        {
            OpenIddictApplicationDescriptor savedClientEntity = null;
            var strategy = GetExecutionStrategy();
            strategy.Execute(() =>
            {
                using (var transaction = _dbContext.Database.BeginTransaction())
                {


                    _manager.CreateAsync(new OpenIddict.Abstractions.OpenIddictApplicationDescriptor
                    {
                        ClientId = client.ClientId_new,
                        ClientSecret = client.ClientSecret,
                        Permissions =
                        {
                            OpenIddictConstants.Permissions.Endpoints.Token,
                            OpenIddictConstants.Permissions.GrantTypes.ClientCredentials
                        }
                    }).GetAwaiter().GetResult();

                    var savedClient = _manager.FindByClientIdAsync("my-client").Result;

                    _dbContext.UserClients.Add(client);
                    _dbContext.SaveChanges();
                    transaction.Commit();
                }
            });
            return client;
        }

        public bool IsClientIdTaken(string clientId)
        {
            var isClientIdAvailable = _dbContext.UserClients.Any(x => x.ClientId_new == clientId);
            return isClientIdAvailable;
        }

        public bool Exists(string id, string userId)
        {
            return _dbContext.UserClients.Any(x => x.ClientId_new == id && x.UserId == userId.ToLower());
        }

        public void Update(string id, string userId, string clientSecret, string returnUrls, string url)
        {
            var strategy = GetExecutionStrategy();
            strategy.Execute(() =>
            {
                using (var transaction = _dbContext.Database.BeginTransaction())
                {
                    var client = _dbContext.UserClients
                        .Where(x => x.UserId == userId.ToLower() && x.ClientId_new == id).FirstOrDefault();
                    if (client != null)
                    {


                        client.ClientSecret = clientSecret;


                        _dbContext.SaveChanges();
                        transaction.Commit();
                    }
                }
            });
        }

        public List<UserClient> GetAllClients()
        {
            return _dbContext.UserClients.ToList();
        }
    }

    public interface IIdentityClientsRepository : ITransaction
    {
        List<UserClient> GetUserRegisteredClients(string userId);

        UserClient GetUserRegisteredClientsById(string id, string userId);

        UserClient AddClient(UserClient client, string userId);

        bool IsClientIdTaken(string clientId);

        bool Exists(string id, string userId);

        void Update(string id, string userId, string clientSecret, string returnUrls, string url);

        List<UserClient> GetAllClients();
    }
}