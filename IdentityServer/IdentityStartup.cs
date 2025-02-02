using ConfigurationManager;
using Google.Cloud.Diagnostics.AspNetCore3;
using Google.Cloud.Diagnostics.Common;
using GoogleTracer;
using IdentityRepo.DbContext;
using IdentityServer.services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using AspNet.Security.OpenId.Steam;
using Google.Cloud.Trace.V1;
using RedisClient;
using TraceServiceOptions = Google.Cloud.Diagnostics.Common.TraceServiceOptions;
using Google;
using Microsoft.AspNetCore.Authentication;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using OpenIddict.Server;
using OpenIddict.Server.AspNetCore;
using OpenIddict.Validation.AspNetCore;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Routing;

namespace IdentityServer
{
    public class IdentityStartup
    {
        public IdentityStartup(IConfiguration configuration)
        {
            Configuration = configuration;

            ServicePointManager
                    .ServerCertificateValidationCallback +=
                (sender, cert, chain, sslPolicyErrors) => true;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            var projectId = Environment.GetEnvironmentVariable("projectId") ?? Configuration.GetValue<string>("AppSetting:projectId");
            var identityCertPass = Environment.GetEnvironmentVariable("IdentityCertPassword") ?? Configuration.GetValue<string>("AppSetting:IdentityCertPassword");
            var identityBaseUrl = Environment.GetEnvironmentVariable("IdentityUrlBase") ?? Configuration.GetValue<string>("AppSetting:IdentityUrlBase");

            //services.AddControllersWithViews();
            services.AddSingleton<AppSetting>();
            services.AddSingleton<IMyRestClient, MyRestClient>();
            services.AddSingleton<IRedisService, RedisService>();
            services.AddScoped<IIdentityClientsRepository, IdentityClientsRepository>();
            services.AddSingleton<IStartupFilter, MigrationStartupFilter<IdentityDbContext>>();
            services.AddSingleton<IRandomGenerator, RandomGenerator>();
            services.AddDbContext<IdentityDbContext>();
            // Register a method that sets the updated trace context information on the response.
            Tracer.SetupTracer(services, projectId, "Identity");

            services.AddDataProtection()
                .SetApplicationName("***")
                .AddKeyManagementOptions(options =>
                {
                    options.AutoGenerateKeys = true;
                    options.NewKeyLifetime = TimeSpan.FromDays(7);
                })
                .PersistKeysToDbContext<IdentityDbContext>();

            services.AddHttpContextAccessor();
            services.AddMvc(x => { x.EnableEndpointRouting = false; });
            services.AddOpenIddict()
            .AddCore(options =>
            {
                options.UseEntityFrameworkCore()
                    .UseDbContext<IdentityDbContext>();

            })
            .AddServer(options =>
            {
                options.SetIssuer(new Uri($"{identityBaseUrl}/identity"));
                options.SetAuthorizationEndpointUris(new Uri($"{identityBaseUrl}/identity/Account/Login"));
                options.SetTokenEndpointUris(new Uri($"{identityBaseUrl}/identity/token"));
                options.SetJsonWebKeySetEndpointUris(new Uri($"{identityBaseUrl}/identity/.well-known/jwks"), new Uri("http://host.docker.internal/identity/.well-known/jwks"));

                options.AllowClientCredentialsFlow();
                options.RegisterScopes(OpenIddictConstants.Scopes.OpenId, OpenIddictConstants.Scopes.Profile);
                var readOnlySpan = File.ReadAllText("/myapp/cert/self-signed-certificate.pem");
                var readAllText = File.ReadAllText("/myapp/cert/private-key.pem");
                //options.AddEncryptionCertificate(X509Certificate2.CreateFromPem(readOnlySpan, readAllText));
                //options.AddSigningCertificate(X509Certificate2.CreateFromPem(readOnlySpan, readAllText));
                options.AddDevelopmentEncryptionCertificate();
                options.AddDevelopmentSigningCertificate();
                options.UseAspNetCore().EnableTokenEndpointPassthrough().DisableTransportSecurityRequirement();

                options.AddEventHandler<OpenIddictServerEvents.HandleConfigurationRequestContext>(builder =>
                    builder.UseInlineHandler(context =>
                    {
                        // Attach custom metadata to the configuration document.
                        context.Metadata["custom_metadata"] = 42;

                        return default;
                    }));
            })
            .AddValidation(options =>
            {
                // Import the configuration from the local OpenIddict server instance.
                options.UseLocalServer();

                // Register the ASP.NET Core host.
                options.UseAspNetCore();

                // Enable authorization entry validation, which is required to be able
                // to reject access tokens retrieved from a revoked authorization code.
                options.EnableAuthorizationEntryValidation();
            });

            services.AddAuthentication(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme);
            services.AddAuthorization();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "My API", Version = "v1" });
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            InitializeDatabase(app);
            app.Map(new PathString("/identity"), (applicationBuilder) =>
            {
                Tracer.CurrentTracer = app.ApplicationServices.GetService<IManagedTracer>();

                applicationBuilder.UseRouting();
                applicationBuilder.UseDefaultFiles();
                applicationBuilder.UseStaticFiles(new StaticFileOptions()
                {
                    OnPrepareResponse = (context) =>
                    {
                        // Disable caching for all static files.
                        context.Context.Response.Headers["Cache-Control"] = "no-cache, no-store";
                        context.Context.Response.Headers["Pragma"] = "no-cache";
                        context.Context.Response.Headers["Expires"] = "-1";
                    }
                });
                /* app.UseForwardedHeaders(new ForwardedHeadersOptions {
                     ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedHost | ForwardedHeaders.XForwardedProto
                 });*/
                applicationBuilder.UseAuthentication();
                applicationBuilder.UseCookiePolicy(
                    new CookiePolicyOptions
                    {
                        Secure = CookieSecurePolicy.Always
                    });

                applicationBuilder.UseAuthentication();
                applicationBuilder.UseAuthorization();

                applicationBuilder.UseSwagger();
                applicationBuilder.UseSwaggerUI(c => { c.SwaggerEndpoint("v1/swagger.json", "My API V1"); });
                applicationBuilder.UseMvc(routes =>
                {
                    routes.MapRoute(
                        name: "default",
                        template: "{controller}/{action}/{id?}");
                });
                applicationBuilder.UseEndpoints(endpoints =>
                {
                    endpoints.MapFallbackToFile("/index.html");
                });

            });
        }

        private void InitializeDatabase(IApplicationBuilder app)
        {
            using (var serviceScope = app.ApplicationServices.GetService<IServiceScopeFactory>().CreateScope())
            {
                var appsettings = serviceScope.ServiceProvider.GetRequiredService<AppSetting>();
                var manager = serviceScope.ServiceProvider.GetRequiredService<OpenIddict.Abstractions.IOpenIddictApplicationManager>();

                var client1 = manager.FindByClientIdAsync(appsettings["ClientId"]).Result;
                if (client1 != null) manager.DeleteAsync(client1).GetAwaiter().GetResult();

                var oldPassiClient = manager.FindByClientIdAsync(appsettings["PassiClientId"]).Result;
                if (oldPassiClient != null) manager.DeleteAsync(oldPassiClient).GetAwaiter().GetResult();

                var oldPgAdminClient = manager.FindByClientIdAsync(appsettings["PgAdminClientId"]).Result;
                if (oldPgAdminClient != null) manager.DeleteAsync(oldPgAdminClient).GetAwaiter().GetResult();

                var oldPassiChatClient = manager.FindByClientIdAsync(appsettings["PassiChatClientId"]).Result;
                if (oldPassiChatClient != null) manager.DeleteAsync(oldPassiChatClient).GetAwaiter().GetResult();

                var oldMailluClient = manager.FindByClientIdAsync(appsettings["MailuClientId"]).Result;
                if (oldMailluClient != null) manager.DeleteAsync(oldMailluClient).GetAwaiter().GetResult();


                manager.CreateAsync(new OpenIddict.Abstractions.OpenIddictApplicationDescriptor
                {
                    ClientId = appsettings["ClientId"],
                    ClientSecret = appsettings["ClientSecret"],
                    RedirectUris = { new Uri("https://localhost/passiweb/oauth/callback"),
                        new Uri("http://host.docker.internal/signin-oidc"),
                        new Uri("https://localhost/signin-oidc"),
                        new Uri("https://localhost/oauth/callback"),
                        new Uri("https://127.0.0.1:5002/oauth/callback"),
                        new Uri("https://localhost:5002/oauth/callback"),
                        new Uri("https://192.168.0.208/oauth/callback"),
                        new Uri("https://passi.cloud/oauth/callback"),
                        new Uri("https://192.168.0.208:5002/oauth/callback") },

                    Permissions =
                    {
                        OpenIddictConstants.Permissions.Endpoints.Token,
                        OpenIddictConstants.Permissions.GrantTypes.ClientCredentials
                    },

                }).GetAwaiter().GetResult();
                manager.CreateAsync(new OpenIddict.Abstractions.OpenIddictApplicationDescriptor
                {
                    ClientId = appsettings["PgAdminClientId"],
                    ClientSecret = appsettings["PgAdminSecret"],
                    RedirectUris =
                    {
                        new Uri("http://localhost/pgadmin4/oauth2/authorize"),
                        new Uri("https://localhost/pgadmin4/oauth2/authorize"),
                        new Uri("http://passi.cloud/pgadmin4/oauth2/authoriz"),
                        new Uri("https://passi.cloud/pgadmin4/oauth2/authorize"),

                    },
                    Permissions =
                    {
                        OpenIddictConstants.Permissions.Endpoints.Token,
                        OpenIddictConstants.Permissions.GrantTypes.ClientCredentials
                    }
                }).GetAwaiter().GetResult();
                manager.CreateAsync(new OpenIddict.Abstractions.OpenIddictApplicationDescriptor
                {
                    ClientId = appsettings["PassiClientId"],
                    ClientSecret = appsettings["PassiSecret"],
                    RedirectUris = {
                        new Uri("https://localhost/passiapi/oauth/callback"),
                        new Uri("https://127.0.0.1:5004/passiapi/oauth/callback"),
                        new Uri("https://localhost:5004/passiapi/oauth/callback"),
                        new Uri("https://192.168.0.208/passiapi/oauth/callback"),
                        new Uri("https://passi.cloud/passiapi/oauth/callback"),
                        new Uri("https://192.168.0.208:5004/passiapi/oauth/callback")

                    },
                    Permissions =
                    {
                        OpenIddictConstants.Permissions.Endpoints.Token,
                        OpenIddictConstants.Permissions.GrantTypes.ClientCredentials
                    }
                }).GetAwaiter().GetResult();
                manager.CreateAsync(new OpenIddict.Abstractions.OpenIddictApplicationDescriptor
                {
                    ClientId = appsettings["PassiChatClientId"],
                    ClientSecret = appsettings["PassiChatSecret"],
                    RedirectUris =
                    {
                        new Uri("myapp://callback"),

                    },
                    Permissions =
                    {
                        OpenIddictConstants.Permissions.Endpoints.Token,
                        OpenIddictConstants.Permissions.GrantTypes.ClientCredentials
                    }
                }).GetAwaiter().GetResult();
                manager.CreateAsync(new OpenIddict.Abstractions.OpenIddictApplicationDescriptor
                {
                    ClientId = appsettings["MailuClientId"],
                    ClientSecret = appsettings["MailluSecret"],
                    RedirectUris =
                    {
                        new Uri("http://localhost/webmail/oauth2/authorize"),
                        new Uri("https://localhost/webmail/oauth2/authorize"),
                        new Uri("http://passi.cloud/webmail/oauth2/authorize"),
                        new Uri("https://passi.cloud/webmail/oauth2/authorize"),

                    },
                    Permissions =
                    {
                        OpenIddictConstants.Permissions.Endpoints.Token,
                        OpenIddictConstants.Permissions.GrantTypes.ClientCredentials
                    }
                }).GetAwaiter().GetResult();

            }
        }
    }
}