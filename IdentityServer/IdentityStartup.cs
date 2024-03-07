using ConfigurationManager;
using Google.Cloud.Diagnostics.AspNetCore3;
using Google.Cloud.Diagnostics.Common;
using GoogleTracer;
using IdentityModel;
using IdentityRepo.DbContext;
using IdentityServer.services;
using IdentityServer4.Configuration;
using IdentityServer4.Models;
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
using System.Security.Cryptography.X509Certificates;
using Google.Cloud.Trace.V1;
using IdentityServer4.Configuration.DependencyInjection;
using IdentityServer4.Configuration.DependencyInjection.BuilderExtensions;
using IdentityServer4.Configuration.DependencyInjection.Options;
using IdentityServer4.EntityFramework;
using IdentityServer4.EntityFramework.Storage.Entities;
using IdentityServer4.EntityFramework.Storage.Mappers;
using IdentityServer4.Storage.Models;

using TraceServiceOptions = Google.Cloud.Diagnostics.Common.TraceServiceOptions;

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
            //services.AddControllersWithViews();
            services.AddSingleton<AppSetting>();
            services.AddScoped<IMyRestClient, MyRestClient>();
            services.AddScoped<IdentityDbContext>();
            services.AddScoped<IIdentityClientsRepository, IdentityClientsRepository>();
            services.AddSingleton<IStartupFilter, MigrationStartupFilter<IdentityDbContext>>();
            services.AddSingleton<IRandomGenerator, RandomGenerator>();

            // Register a method that sets the updated trace context information on the response.
            Tracer.SetupTracer(services, projectId, "Identity");

            byte[] certData = File.ReadAllBytes("/myapp/cert/your_certificate.pfx");

            services.AddIdentityServer(options =>
                {
                    options.UserInteraction = new UserInteractionOptions() { ConsentUrl = "/Account/Login", LoginUrl = "/Account/Login" };
                    options.Authentication = new IdentityServer4.Configuration.DependencyInjection.Options.AuthenticationOptions()
                    {
                        CookieSlidingExpiration = true,
                    };
                })
                .AddSigningCredential(new X509Certificate2(certData, identityCertPass))
                .AddProfileService<MyProfileService>()
                .AddUserSession<UserSession>()
                .AddConfigurationStore<IdentityDbContext>()
                //.AddDeveloperSigningCredential()
                .AddOperationalStore<IdentityDbContext>();
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

                var appSetting = applicationBuilder.ApplicationServices.GetService<AppSetting>();
                applicationBuilder.UseRouting();

                applicationBuilder.UseMiddleware<PublicFacingUrlMiddleware>(appSetting["IdentityUrlBase"]);
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
                applicationBuilder.UseIdentityServer();
                applicationBuilder.UseAuthentication();
                applicationBuilder.UseCookiePolicy(
                    new CookiePolicyOptions
                    {
                        Secure = CookieSecurePolicy.Always
                    });
                //applicationBuilder.UseEndpoints(endpoints =>
                //{
                //    endpoints.MapControllerRoute(
                //        name: "default",
                //        pattern: "{controller=Home}/{action=Index}/{id?}");
                //});
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
                var context = serviceScope.ServiceProvider.GetRequiredService<IdentityDbContext>();

                context.Clients.RemoveRange(context.Clients.Where(x => x.ClientId == appsettings["ClientId"])
                    .Include(x => x.RedirectUris).Include(x => x.PostLogoutRedirectUris)
                    .Include(x => x.AllowedScopes)
                    .Include(x => x.ClientSecrets));
                context.UserClients.RemoveRange(
                    context.UserClients.Where(x => x.Client.ClientId == appsettings["ClientId"]));

                context.Clients.RemoveRange(context.Clients.Where(x => x.ClientId == appsettings["PassiClientId"])
                    .Include(x => x.RedirectUris).Include(x => x.PostLogoutRedirectUris)
                    .Include(x => x.AllowedScopes)
                    .Include(x => x.ClientSecrets));
                context.UserClients.RemoveRange(
                    context.UserClients.Where(x => x.Client.ClientId == appsettings["PassiClientId"]));

                var includableQueryable = context.Clients.Where(x => x.ClientId == appsettings["PgAdminClientId"])
                    .Include(x => x.RedirectUris).Include(x => x.PostLogoutRedirectUris)
                    .Include(x => x.AllowedScopes)
                    .Include(x => x.ClientSecrets).ToList();
                context.Clients.RemoveRange(includableQueryable);
                context.UserClients.RemoveRange(
                    context.UserClients.Where(x => x.Client.ClientId == appsettings["PgAdminClientId"]));

                var client = new IdentityServer4.EntityFramework.Storage.Entities.Client()
                {
                    ClientId = appsettings["ClientId"],
                    ClientSecrets = new List<ClientSecret>()
                    {
                        new ClientSecret() { Value = appsettings["ClientSecret"].ToSha256() }
                    },
                    RedirectUris = new List<ClientRedirectUri>()
                    {
                        new ClientRedirectUri(){RedirectUri = "https://localhost/passiweb/oauth/callback"},
                        new ClientRedirectUri(){RedirectUri = "https://localhost/oauth/callback"},
                        new ClientRedirectUri(){RedirectUri = "https://127.0.0.1:5002/oauth/callback"},
                        new ClientRedirectUri(){RedirectUri = "https://localhost:5002/oauth/callback"},
                        new ClientRedirectUri(){RedirectUri = "https://192.168.0.208/oauth/callback"},
                        new ClientRedirectUri(){RedirectUri = "https://passi.cloud/oauth/callback"},
                        new ClientRedirectUri(){RedirectUri = "https://192.168.0.208:5002/oauth/callback"}
                    },
                    RequirePkce = false,
                    AllowedGrantTypes = new List<ClientGrantType>()
                    {
                        new ClientGrantType(){GrantType =GrantType.AuthorizationCode },
                        new ClientGrantType(){GrantType =GrantType.ClientCredentials }
                    },
                    AllowedScopes = new List<ClientScope>() {
                        new ClientScope(){Scope = "openid"} ,
                        new ClientScope(){Scope = "email"},
                        new ClientScope(){Scope = "profile"}
                    },
                    AlwaysIncludeUserClaimsInIdToken = true,
                    ClientUri = "https://passi.cloud",
                    AlwaysSendClientClaims = true,
                };
                context.Clients.Add(client);
                var client2 = new IdentityServer4.EntityFramework.Storage.Entities.Client()
                {
                    ClientId = appsettings["PassiClientId"],
                    ClientSecrets = new List<ClientSecret>() { new ClientSecret() { Value = appsettings["PassiSecret"].ToSha256() } },
                    RedirectUris = new List<ClientRedirectUri>()
                    {
                        new ClientRedirectUri(){RedirectUri = "https://localhost/passiapi/oauth/callback"},
                        new ClientRedirectUri(){RedirectUri = "https://127.0.0.1:5004/passiapi/oauth/callback"},
                        new ClientRedirectUri(){RedirectUri = "https://localhost:5004/passiapi/oauth/callback"},
                        new ClientRedirectUri(){RedirectUri ="https://192.168.0.208/passiapi/oauth/callback"},
                        new ClientRedirectUri(){RedirectUri = "https://passi.cloud/passiapi/oauth/callback"},
                        new ClientRedirectUri(){RedirectUri = "https://192.168.0.208:5004/passiapi/oauth/callback"},
                    },

                    RequirePkce = false,
                    AllowedGrantTypes = new List<ClientGrantType>()
                    {
                        new ClientGrantType(){GrantType =GrantType.AuthorizationCode },
                        new ClientGrantType(){GrantType =GrantType.ClientCredentials }
                    },
                    AllowedScopes = new List<ClientScope>() {
                        new ClientScope(){Scope = "openid"} ,
                        new ClientScope(){Scope = "email"},
                        new ClientScope(){Scope = "profile"}
                    },
                    AlwaysIncludeUserClaimsInIdToken = true,
                    ClientUri = "https://passi.cloud",
                    AlwaysSendClientClaims = true,
                };
                context.Clients.Add(client2);

                var client3 = new IdentityServer4.EntityFramework.Storage.Entities.Client()
                {
                    ClientId = appsettings["PgAdminClientId"],
                    ClientSecrets = new List<ClientSecret>() { new ClientSecret() { Value = appsettings["PgAdminSecret"].ToSha256() } },
                    RedirectUris = new List<ClientRedirectUri>()
                    {
                        new ClientRedirectUri(){RedirectUri = "http://localhost/pgadmin4/oauth2/authorize"},
                        new ClientRedirectUri(){RedirectUri = "https://localhost/pgadmin4/oauth2/authorize"},
                        new ClientRedirectUri(){RedirectUri ="http://passi.cloud/pgadmin4/oauth2/authorize"},
                        new ClientRedirectUri(){RedirectUri ="https://passi.cloud/pgadmin4/oauth2/authorize"}
                    },

                    RequirePkce = false,
                    AllowedGrantTypes = new List<ClientGrantType>()
                    {
                        new ClientGrantType(){GrantType =GrantType.AuthorizationCode },
                        new ClientGrantType(){GrantType =GrantType.ClientCredentials }
                    },
                    AllowedScopes = new List<ClientScope>() {
                        new ClientScope(){Scope = "openid"} ,
                        new ClientScope(){Scope = "email"},
                        new ClientScope(){Scope = "profile"}
                    },
                    AlwaysIncludeUserClaimsInIdToken = true,
                    ClientUri = "https://passi.cloud/pgadmin4",
                    AlwaysSendClientClaims = true,
                };
                context.Clients.Add(client3);

                if (!context.IdentityResources.Any())
                    context.IdentityResources.AddRange(new List<IdentityServer4.EntityFramework.Storage.Entities.IdentityResource>()
                {
                    new IdentityResources.OpenId().ToEntity(),
                });
                context.UserClients.Add(new UserClient() { Client = client, UserId = "your@email.com" });

                var scope = context.ApiScopes.FirstOrDefault(x => x.Name == "email");
                if (scope == null)
                    context.ApiScopes.Add(new IdentityServer4.EntityFramework.Storage.Entities.ApiScope() { Name = "email", Enabled = true, DisplayName = "email" });
                var profileScope = context.ApiScopes.FirstOrDefault(x => x.Name == "profile");
                if (profileScope != null)
                    context.ApiScopes.Remove(profileScope);

                context.SaveChanges();
            }
        }
    }
}