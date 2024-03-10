// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using IdentityServer4.Configuration.DependencyInjection.Options;
using IdentityServer4.Services;
using IdentityServer4.Services.Default;
using IdentityServer4.Storage.Services;
using IdentityServer4.Storage.Stores;
using IdentityServer4.Stores;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

using System;
using System.Net.Http;

namespace IdentityServer4.Configuration.DependencyInjection.BuilderExtensions
{
    /// <summary>
    /// Builder extension methods for registering additional services
    /// </summary>
    [GoogleTracer.Profile]
    public static class IdentityServerBuilderExtensionsAdditional
    {
        /// <summary>
        /// Adds the profile service.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="builder">The builder.</param>
        /// <returns></returns>
        public static IIdentityServerBuilder AddProfileService<T>(this IIdentityServerBuilder builder)
           where T : class, IProfileService
        {
            builder.Services.AddTransient<IProfileService, T>();

            return builder;
        }

        /// <summary>
        /// Adds a client store.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="builder">The builder.</param>
        /// <returns></returns>
        public static IIdentityServerBuilder AddClientStore<T>(this IIdentityServerBuilder builder)
           where T : class, IClientStore
        {
            builder.Services.TryAddTransient(typeof(T));
            builder.Services.AddTransient<IClientStore, ValidatingClientStore<T>>();

            return builder;
        }

        /// <summary>
        /// Adds a resource store.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="builder">The builder.</param>
        /// <returns></returns>
        public static IIdentityServerBuilder AddResourceStore<T>(this IIdentityServerBuilder builder)
           where T : class, IResourceStore
        {
            builder.Services.AddTransient<IResourceStore, T>();

            return builder;
        }

        /// <summary>
        /// Adds a CORS policy service.
        /// </summary>
        /// <typeparam name="T">The type of the concrete scope store class that is registered in DI.</typeparam>
        /// <param name="builder">The builder.</param>
        /// <returns></returns>
        public static IIdentityServerBuilder AddCorsPolicyService<T>(this IIdentityServerBuilder builder)
            where T : class, ICorsPolicyService
        {
            builder.Services.AddTransient<ICorsPolicyService, T>();
            return builder;
        }

        // todo: check with later previews of ASP.NET Core if this is still required
        /// <summary>
        /// Adds configuration for the HttpClient used for back-channel logout notifications.
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <param name="configureClient">The configruation callback.</param>
        /// <returns></returns>
        public static IHttpClientBuilder AddBackChannelLogoutHttpClient(this IIdentityServerBuilder builder, Action<HttpClient> configureClient = null)
        {
            const string name = IdentityServerConstants.HttpClients.BackChannelLogoutHttpClient;
            IHttpClientBuilder httpBuilder;

            if (configureClient != null)
            {
                httpBuilder = builder.Services.AddHttpClient(name, configureClient);
            }
            else
            {
                httpBuilder = builder.Services.AddHttpClient(name)
                    .ConfigureHttpClient(client =>
                    {
                        client.Timeout = TimeSpan.FromSeconds(IdentityServerConstants.HttpClients.DefaultTimeoutSeconds);
                    });
            }

            builder.Services.AddTransient<IBackChannelLogoutHttpClient>(s =>
            {
                var httpClientFactory = s.GetRequiredService<IHttpClientFactory>();
                var httpClient = httpClientFactory.CreateClient(name);
                var loggerFactory = s.GetRequiredService<ILoggerFactory>();

                return new DefaultBackChannelLogoutHttpClient(httpClient, loggerFactory);
            });

            return httpBuilder;
        }

        // todo: check with later previews of ASP.NET Core if this is still required
        /// <summary>
        /// Adds configuration for the HttpClient used for JWT request_uri requests.
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <param name="configureClient">The configruation callback.</param>
        /// <returns></returns>
        public static IHttpClientBuilder AddJwtRequestUriHttpClient(this IIdentityServerBuilder builder, Action<HttpClient> configureClient = null)
        {
            const string name = IdentityServerConstants.HttpClients.JwtRequestUriHttpClient;
            IHttpClientBuilder httpBuilder;

            if (configureClient != null)
            {
                httpBuilder = builder.Services.AddHttpClient(name, configureClient);
            }
            else
            {
                httpBuilder = builder.Services.AddHttpClient(name)
                    .ConfigureHttpClient(client =>
                    {
                        client.Timeout = TimeSpan.FromSeconds(IdentityServerConstants.HttpClients.DefaultTimeoutSeconds);
                    });
            }

            builder.Services.AddTransient<IJwtRequestUriHttpClient, DefaultJwtRequestUriHttpClient>(s =>
            {
                var httpClientFactory = s.GetRequiredService<IHttpClientFactory>();
                var httpClient = httpClientFactory.CreateClient(name);
                var loggerFactory = s.GetRequiredService<ILoggerFactory>();
                var options = s.GetRequiredService<IdentityServerOptions>();

                return new DefaultJwtRequestUriHttpClient(httpClient, options, loggerFactory);
            });

            return httpBuilder;
        }

        /// <summary>
        /// Adds a custom user session.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="builder">The builder.</param>
        /// <returns></returns>
        public static IIdentityServerBuilder AddUserSession<T>(this IIdentityServerBuilder builder)
            where T : class, IUserSession
        {
            // This is added as scoped due to the note regarding the AuthenticateAsync
            // method in the IdentityServer4.Services.DefaultUserSession implementation.
            builder.Services.AddScoped<IUserSession, T>();

            return builder;
        }
    }
}