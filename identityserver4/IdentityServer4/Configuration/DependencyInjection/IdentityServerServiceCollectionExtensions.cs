// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using IdentityServer4.Configuration.DependencyInjection.BuilderExtensions;
using IdentityServer4.Configuration.DependencyInjection.Options;
using Microsoft.Extensions.DependencyInjection;

using System;

namespace IdentityServer4.Configuration.DependencyInjection
{
    /// <summary>
    /// DI extension methods for adding IdentityServer
    /// </summary>
    [GoogleTracer.Profile]
    public static class IdentityServerServiceCollectionExtensions
    {
        /// <summary>
        /// Creates a builder.
        /// </summary>
        /// <param name="services">The services.</param>
        /// <returns></returns>
        public static IIdentityServerBuilder AddIdentityServerBuilder(this IServiceCollection services)
        {
            return new IdentityServerBuilder(services);
        }

        /// <summary>
        /// Adds IdentityServer.
        /// </summary>
        /// <param name="services">The services.</param>
        /// <returns></returns>
        public static IIdentityServerBuilder AddIdentityServer(this IServiceCollection services)
        {
            var builder = services.AddIdentityServerBuilder();

            builder
                .AddRequiredPlatformServices()
                .AddCookieAuthentication()
                .AddCoreServices()
                .AddDefaultEndpoints()
                .AddPluggableServices()
                .AddValidators()
                .AddResponseGenerators()
                .AddDefaultSecretParsers()
                .AddDefaultSecretValidators();

            return builder;
        }

        /// <summary>
        /// Adds IdentityServer.
        /// </summary>
        /// <param name="services">The services.</param>
        /// <param name="setupAction">The setup action.</param>
        /// <returns></returns>
        public static IIdentityServerBuilder AddIdentityServer(this IServiceCollection services, Action<IdentityServerOptions> setupAction)
        {
            services.Configure(setupAction);
            return services.AddIdentityServer();
        }
    }
}