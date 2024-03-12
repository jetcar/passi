// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using GoogleTracer;
using IdentityServer4.EntityFramework.Storage.Interfaces;
using IdentityServer4.EntityFramework.Storage.Mappers;
using IdentityServer4.Storage.Models;
using IdentityServer4.Storage.Stores;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RedisClient;

namespace IdentityServer4.EntityFramework.Storage.Stores
{
    /// <summary>
    /// Implementation of IClientStore thats uses EF.
    /// </summary>
    /// <seealso cref="IClientStore" />
    [Profile]
    public class ClientStore : IClientStore
    {
        /// <summary>
        /// The DbContext.
        /// </summary>
        protected readonly IConfigurationDbContext Context;

        /// <summary>
        /// The logger.
        /// </summary>
        protected readonly ILogger<ClientStore> Logger;

        private IRedisService _redisService;

        /// <summary>
        /// Initializes a new instance of the <see cref="ClientStore"/> class.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="logger">The logger.</param>
        /// <exception cref="ArgumentNullException">context</exception>
        public ClientStore(IConfigurationDbContext context, ILogger<ClientStore> logger, IRedisService redisService)
        {
            Context = context ?? throw new ArgumentNullException(nameof(context));
            Logger = logger;
            _redisService = redisService;
        }

        /// <summary>
        /// Finds a client by id
        /// </summary>
        /// <param name="clientId">The client id</param>
        /// <returns>
        /// The client
        /// </returns>
        public virtual async Task<Client> FindClientByIdAsync(string clientId)
        {
            var redisValue = _redisService.Get<Client>(clientId);
            if (redisValue != null)
            {
                Logger.LogDebug("{clientId} found in redis", clientId);
                return redisValue;
            }
            var baseQuery = Context.Clients
                .Where(x => x.ClientId == clientId);

            var client = baseQuery.AsQueryable().SingleOrDefault(x => x.ClientId == clientId);
            if (client == null) return null;

            await baseQuery.Include(x => x.AllowedGrantTypes).SelectMany(c => c.AllowedGrantTypes).AsQueryable().LoadAsync();
            await baseQuery.Include(x => x.AllowedScopes).SelectMany(c => c.AllowedScopes).LoadAsync();
            await baseQuery.Include(x => x.ClientSecrets).SelectMany(c => c.ClientSecrets).LoadAsync();
            await baseQuery.Include(x => x.RedirectUris).SelectMany(c => c.RedirectUris).LoadAsync();

            var model = client.ToModel();
            _redisService.Add(model.ClientId, model, new TimeSpan(1, 0, 0));
            Logger.LogDebug("{clientId} found in database: {clientIdFound}", clientId, model);

            return model;
        }
    }
}