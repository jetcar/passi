using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using ConfigurationManager;
using IdentityModel;
using IdentityServer.DbContext;
using IdentityServer.services;
using IdentityServer4.Configuration;
using IdentityServer4.EntityFramework.Mappers;
using IdentityServer4.Models;
using IdentityServer4.Validation;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using AuthenticationOptions = IdentityServer4.Configuration.AuthenticationOptions;
using IdentityResource = IdentityServer4.EntityFramework.Entities.IdentityResource;
using Microsoft.AspNetCore.Identity;

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
            services.AddControllersWithViews();
            services.AddSingleton<AppSetting>();
            services.AddScoped<IdentityDbContext>();
            services.AddScoped<IIdentityClientsRepository, IdentityClientsRepository>();
            services.AddSingleton<IStartupFilter, MigrationStartupFilter<IdentityDbContext>>();

            services.AddIdentityServer(options =>
                {
                    options.UserInteraction = new UserInteractionOptions() { ConsentUrl = "/Account/Login", LoginUrl = "/Account/Login" };
                    options.Authentication = new AuthenticationOptions()
                    {
                        CookieSlidingExpiration = true,
                    };
                })
                .AddProfileService<MyProfileService>()
                .AddUserSession<UserSession>()
                .AddConfigurationStore<IdentityDbContext>()
                .AddDeveloperSigningCredential()
                .AddOperationalStore<IdentityDbContext>();
            services.AddDataProtection()
                .SetApplicationName("***")
                .AddKeyManagementOptions(options =>
                {
                    options.AutoGenerateKeys = true;
                    options.NewKeyLifetime = TimeSpan.FromDays(7);
                })
                .PersistKeysToDbContext<IdentityDbContext>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            InitializeDatabase(app);
            app.Map(new PathString("/identity"), (applicationBuilder) =>
            {
                if (env.IsDevelopment())
                {
                    applicationBuilder.UseDeveloperExceptionPage();
                }
                else
                {
                    applicationBuilder.UseExceptionHandler("/Home/Error");
                    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                }

                var appSetting = applicationBuilder.ApplicationServices.GetService<AppSetting>();
                applicationBuilder.UseMiddleware<PublicFacingUrlMiddleware>(appSetting["IdentityUrlBase"]);

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
                applicationBuilder.UseRouting();
                applicationBuilder.UseIdentityServer();
                applicationBuilder.UseAuthorization();
                applicationBuilder.UseCookiePolicy();

                applicationBuilder.UseEndpoints(endpoints =>
                {
                    endpoints.MapControllerRoute(
                        name: "default",
                        pattern: "{controller=Home}/{action=Index}/{id?}");
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

                    context.Clients.RemoveRange(context.Clients.Where(x => x.ClientId == appsettings["PgAdminClientId"])
                        .Include(x => x.RedirectUris).Include(x => x.PostLogoutRedirectUris)
                        .Include(x => x.AllowedScopes)
                        .Include(x => x.ClientSecrets));
                    context.UserClients.RemoveRange(
                        context.UserClients.Where(x => x.Client.ClientId == appsettings["PgAdminClientId"]));
                

                var client = new Client()
                {
                    ClientId = appsettings["ClientId"],
                    ClientSecrets = new List<IdentityServer4.Models.Secret>() { new IdentityServer4.Models.Secret() { Value = appsettings["ClientSecret"].ToSha256() } },
                    RedirectUris = new List<string>() { "https://localhost/passiweb/oauth/callback", "https://localhost/oauth/callback", "https://127.0.0.1:5002/oauth/callback", "https://localhost:5002/oauth/callback", "https://192.168.0.208/oauth/callback", "https://passi.cloud/oauth/callback", "https://192.168.0.208:5002/oauth/callback" },
                    PostLogoutRedirectUris = new List<string>() { "https://localhost", "https://passi.cloud" },
                    RequirePkce = false,
                    AllowedGrantTypes = GrantTypes.CodeAndClientCredentials,
                    AllowedScopes = new List<string>() { "openid" },
                    AlwaysIncludeUserClaimsInIdToken = true,
                    ClientUri = "https://passi.cloud",
                    AlwaysSendClientClaims = true,
                }.ToEntity();
                context.Clients.Add(client);
                var client2 = new Client()
                {
                    ClientId = appsettings["PassiClientId"],
                    ClientSecrets = new List<IdentityServer4.Models.Secret>() { new IdentityServer4.Models.Secret() { Value = appsettings["PassiSecret"].ToSha256() } },
                    RedirectUris = new List<string>() { "https://localhost/passiapi/oauth/callback", "https://127.0.0.1:5004/passiapi/oauth/callback", "https://localhost:5004/passiapi/oauth/callback", "https://192.168.0.208/passiapi/oauth/callback", "https://passi.cloud/passiapi/oauth/callback", "https://192.168.0.208:5004/passiapi/oauth/callback" },
                    PostLogoutRedirectUris = new List<string>() { "https://localhost", "https://passi.cloud" },
                    RequirePkce = false,
                    AllowedGrantTypes = GrantTypes.CodeAndClientCredentials,
                    AllowedScopes = new List<string>() { "openid" },
                    AlwaysIncludeUserClaimsInIdToken = true,
                    ClientUri = "https://passi.cloud",
                    AlwaysSendClientClaims = true,
                }.ToEntity();
                context.Clients.Add(client2);

                var client3 = new Client()
                {
                    ClientId = appsettings["PgAdminClientId"],
                    ClientSecrets = new List<IdentityServer4.Models.Secret>() { new IdentityServer4.Models.Secret() { Value = appsettings["PgAdminSecret"].ToSha256() } },
                    RedirectUris = new List<string>() { "https://localhost/pgadmin/oauth/callback", "https://127.0.0.1:5004/pgadmin/oauth/callback", "https://localhost:5004/pgadmin/oauth/callback", "https://192.168.0.208/pgadmin/oauth/callback", "https://passi.cloud/pgadmin/oauth/callback", "https://192.168.0.208:5004/pgadmin/oauth/callback" },
                    PostLogoutRedirectUris = new List<string>() { "https://localhost", "https://passi.cloud" },
                    RequirePkce = false,
                    AllowedGrantTypes = GrantTypes.CodeAndClientCredentials,
                    AllowedScopes = new List<string>() { "openid","email" },
                    AlwaysIncludeUserClaimsInIdToken = true,
                    ClientUri = "https://passi.cloud/pgadmin",
                    AlwaysSendClientClaims = true,
                }.ToEntity();
                context.Clients.Add(client3);

                if (!context.IdentityResources.Any())
                    context.IdentityResources.AddRange(new List<IdentityResource>()
                {
                    new IdentityResources.OpenId().ToEntity(),
                });
                context.UserClients.Add(new UserClient() { Client = client, UserId = "your@email.com" });

                context.SaveChanges();
            }
        }
    }
}