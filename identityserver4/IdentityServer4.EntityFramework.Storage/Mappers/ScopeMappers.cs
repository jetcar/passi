// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using AutoMapper;
using IdentityServer4.Storage.Models;

namespace IdentityServer4.EntityFramework.Storage.Mappers
{
    /// <summary>
    /// Extension methods to map to/from entity/model for scopes.
    /// </summary>
    [GoogleTracer.Profile]
    public static class ScopeMappers
    {
        static ScopeMappers()
        {
            Mapper = new MapperConfiguration(cfg => cfg.AddProfile<ScopeMapperProfile>())
                .CreateMapper();
        }

        public static IMapper Mapper { get; }

        /// <summary>
        /// Maps an entity to a model.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <returns></returns>
        public static ApiScope ToModel(this Entities.ApiScope entity)
        {
            return entity == null ? null : Mapper.Map<ApiScope>(entity);
        }
    }
}