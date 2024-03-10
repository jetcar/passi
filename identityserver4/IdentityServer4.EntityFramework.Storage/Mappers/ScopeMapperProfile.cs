// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System.Collections.Generic;
using AutoMapper;
using IdentityServer4.Storage.Models;

namespace IdentityServer4.EntityFramework.Storage.Mappers
{
    /// <summary>
    /// Defines entity/model mapping for scopes.
    /// </summary>
    /// <seealso cref="AutoMapper.Profile" />
    [GoogleTracer.Profile]
    public class ScopeMapperProfile : Profile
    {
        /// <summary>
        /// <see cref="ScopeMapperProfile"/>
        /// </summary>
        public ScopeMapperProfile()
        {
            CreateMap<Entities.ApiScopeProperty, KeyValuePair<string, string>>()
                .ReverseMap();

            CreateMap<Entities.ApiScopeClaim, string>()
               .ConstructUsing(x => x.Type)
               .ReverseMap()
               .ForMember(dest => dest.Type, opt => opt.MapFrom(src => src));

            CreateMap<Entities.ApiScope, ApiScope>(MemberList.Destination)
                .ConstructUsing(src => new ApiScope())
                .ForMember(x => x.Properties, opts => opts.MapFrom(x => x.Properties))
                .ForMember(x => x.UserClaims, opts => opts.MapFrom(x => x.UserClaims))
                .ReverseMap();
        }
    }
}