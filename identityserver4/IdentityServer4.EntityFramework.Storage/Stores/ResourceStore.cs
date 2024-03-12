using System;
using System.Collections.Generic;
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
using static System.Formats.Asn1.AsnWriter;

namespace IdentityServer4.EntityFramework.Storage.Stores
{
    /// <summary>
    /// Implementation of IResourceStore thats uses EF.
    /// </summary>
    /// <seealso cref="IResourceStore" />
    [Profile]
    public class ResourceStore : IResourceStore
    {
        /// <summary>
        /// The DbContext.
        /// </summary>
        protected readonly IConfigurationDbContext Context;

        private IRedisService _redisService;

        /// <summary>
        /// The logger.
        /// </summary>
        protected readonly ILogger<ResourceStore> Logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ResourceStore"/> class.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="logger">The logger.</param>
        /// <exception cref="ArgumentNullException">context</exception>
        public ResourceStore(IConfigurationDbContext context, ILogger<ResourceStore> logger, IRedisService redisService)
        {
            Context = context ?? throw new ArgumentNullException(nameof(context));
            Logger = logger;
            _redisService = redisService;
        }

        /// <summary>
        /// Finds the API resources by name.
        /// </summary>
        /// <param name="apiResourceNames">The names.</param>
        /// <returns></returns>
        public virtual async Task<IEnumerable<ApiResource>> FindApiResourcesByNameAsync(IEnumerable<string> apiResourceNames)
        {
            if (apiResourceNames == null) throw new ArgumentNullException(nameof(apiResourceNames));

            var resourceNames = apiResourceNames as string[] ?? apiResourceNames.ToArray();
            var redisValue = _redisService.Get<ApiResource[]>(string.Join(",", resourceNames.ToArray()));
            if (redisValue != null)
                return redisValue;

            var query =
                from apiResource in Context.ApiResources
                where resourceNames.Contains(apiResource.Name)
                select apiResource;

            var apis = query
                .Include(x => x.Secrets)
                .Include(x => x.Scopes)
                .Include(x => x.UserClaims)
                .Include(x => x.Properties)
                .AsNoTracking();

            var result = (await apis.ToArrayAsync())
                .Where(x => resourceNames.Contains(x.Name))
                .Select(x => x.ToModel()).ToArray();

            if (result.Any())
            {
                Logger.LogDebug("Found {apis} API resource in database", result.Select(x => x.Name));
            }
            else
            {
                Logger.LogDebug("Did not find {apis} API resource in database", apiResourceNames);
            }
            _redisService.Add(string.Join(",", resourceNames), result, new TimeSpan(1, 0, 0));

            return result;
        }

        /// <summary>
        /// Gets API resources by scope name.
        /// </summary>
        /// <param name="scopeNames"></param>
        /// <returns></returns>
        public virtual async Task<IEnumerable<ApiResource>> FindApiResourcesByScopeNameAsync(IEnumerable<string> scopeNames)
        {
            var names = scopeNames.ToArray();
            var redisValue = _redisService.Get<ApiResource[]>(string.Join(",", names));
            if (redisValue != null)
                return redisValue;

            var query =
                from api in Context.ApiResources
                where api.Scopes.Where(x => names.Contains(x.Scope)).Any()
                select api;

            var apis = query
                .Include(x => x.Secrets)
                .Include(x => x.Scopes)
                .Include(x => x.UserClaims)
                .AsNoTracking();

            var results = (await apis.ToArrayAsync())
                .Where(api => api.Scopes.Any(x => names.Contains(x.Scope)));
            var models = results.Select(x => x.ToModel()).ToArray();
            _redisService.Add(string.Join(",", names), models, new TimeSpan(1, 0, 0));

            Logger.LogDebug("Found {apis} API resources in database", models.Select(x => x.Name));

            return models;
        }

        /// <summary>
        /// Gets identity resources by scope name.
        /// </summary>
        /// <param name="scopeNames"></param>
        /// <returns></returns>
        public virtual async Task<IEnumerable<IdentityResource>> FindIdentityResourcesByScopeNameAsync(IEnumerable<string> scopeNames)
        {
            var scopes = scopeNames.ToArray();
            var redisValue = _redisService.Get<IdentityResource[]>(string.Join(",", scopes));
            if (redisValue != null)
                return redisValue;
            var query =
                from identityResource in Context.IdentityResources
                where scopes.Contains(identityResource.Name)
                select identityResource;

            var resources = query
                .Include(x => x.UserClaims)
                .AsNoTracking();

            var results = (await resources.ToArrayAsync())
                .Where(x => scopes.Contains(x.Name));

            Logger.LogDebug("Found {scopes} identity scopes in database", results.Select(x => x.Name));

            var findIdentityResourcesByScopeNameAsync = results.Select(x => x.ToModel()).ToArray();
            _redisService.Add(string.Join(",", scopes), findIdentityResourcesByScopeNameAsync, new TimeSpan(1, 0, 0));
            return findIdentityResourcesByScopeNameAsync;
        }

        /// <summary>
        /// Gets scopes by scope name.
        /// </summary>
        /// <param name="scopeNames"></param>
        /// <returns></returns>
        public virtual async Task<IEnumerable<ApiScope>> FindApiScopesByNameAsync(IEnumerable<string> scopeNames)
        {
            var scopes = scopeNames.ToArray();
            var redisValue = _redisService.Get<ApiScope[]>(string.Join(",", scopes));
            if (redisValue != null)
                return redisValue;

            var query =
                from scope in Context.ApiScopes
                where scopes.Contains(scope.Name)
                select scope;

            var resources = query
                .Include(x => x.UserClaims)
                .Include(x => x.Properties)
                .AsNoTracking();

            var results = (await resources.ToArrayAsync())
                .Where(x => scopes.Contains(x.Name));

            Logger.LogDebug("Found {scopes} scopes in database", results.Select(x => x.Name));

            var findApiScopesByNameAsync = results.Select(x => x.ToModel()).ToArray();
            _redisService.Add(string.Join(",", scopes), findApiScopesByNameAsync, new TimeSpan(1, 0, 0));

            return findApiScopesByNameAsync;
        }

        /// <summary>
        /// Gets all resources.
        /// </summary>
        /// <returns></returns>
        public virtual async Task<Resources> GetAllResourcesAsync()
        {
            var redisValue = _redisService.Get<Resources>("");
            if (redisValue != null)
                return redisValue;

            var identity = Context.IdentityResources
              .Include(x => x.UserClaims)
              .Include(x => x.Properties);

            var apis = Context.ApiResources
                .Include(x => x.Secrets)
                .Include(x => x.Scopes)
                .Include(x => x.UserClaims)
                .Include(x => x.Properties)
                .AsNoTracking();

            var scopes = Context.ApiScopes
                .Include(x => x.UserClaims)
                .Include(x => x.Properties)
                .AsNoTracking();

            var result = new Resources(
                (await identity.ToArrayAsync()).Select(x => x.ToModel()),
                (await scopes.ToArrayAsync()).Select(x => x.ToModel())
            );

            Logger.LogDebug("Found {scopes} as all scopes, and {apis} as API resources",
                result.IdentityResources.Select(x => x.Name).Union(result.ApiScopes.Select(x => x.Name)),
                result.ApiResources.Select(x => x.Name));
            _redisService.Add("", result, new TimeSpan(1, 0, 0));

            return result;
        }
    }
}