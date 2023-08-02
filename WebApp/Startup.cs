using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Diagnostics;
using ConfigurationManager;
using Microsoft.AspNetCore.DataProtection;
using OpenIdLib.OpenId;
using Serilog;
using Serilog.Events;
using Services;
using Google.Cloud.Diagnostics.AspNetCore3;
using Google.Cloud.Diagnostics.Common;
using System.Collections.Generic;

namespace WebApp
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            var passiUrl = Environment.GetEnvironmentVariable("PassiUrl") ?? Configuration.GetValue<string>("AppSetting:PassiUrl");
            var identityUrl = Environment.GetEnvironmentVariable("IdentityUrl") ?? Configuration.GetValue<string>("AppSetting:IdentityUrl");
            var returnUrl = Environment.GetEnvironmentVariable("returnUrl") ?? Configuration.GetValue<string>("AppSetting:returnUrl");
            var clientId = Environment.GetEnvironmentVariable("ClientId") ?? Configuration.GetValue<string>("AppSetting:ClientId");
            var secret = Environment.GetEnvironmentVariable("ClientSecret") ?? Configuration.GetValue<string>("AppSetting:ClientSecret");
            var projectId = Environment.GetEnvironmentVariable("projectId") ?? Configuration.GetValue<string>("AppSetting:projectId");


            services.AddGoogleTraceForAspNetCore(new AspNetCoreTraceOptions
            {
                ServiceOptions = new TraceServiceOptions()
                {
                    ProjectId = "passi-165ca"
                }
            }); services.AddSingleton<AppSetting>();
            services.AddScoped<WebAppDbContext>();
            services.AddSingleton<IStartupFilter, MigrationStartupFilter<WebAppDbContext>>();
            var myRestClient = new MyRestClient(passiUrl);
            services.AddSingleton<IMyRestClient>(myRestClient);
            services.AddDataProtection()
                .SetApplicationName("WebApp")
                .AddKeyManagementOptions(options =>
                {
                    options.AutoGenerateKeys = true;
                    options.NewKeyLifetime = TimeSpan.FromDays(7);
                })
                .PersistKeysToDbContext<WebAppDbContext>();

            services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = "Cookies";
                })
                .AddCookie("Cookies",
                    options =>
                    {
                        options.AccessDeniedPath = "/Home/Error";
                        options.SlidingExpiration = true;
                        options.ExpireTimeSpan = System.TimeSpan.FromDays(30);
                    })
                .AddOpenIdAuthentication(identityUrl, returnUrl, passiUrl, clientId, secret, myRestClient)
                .AddOpenIdTokenManagement(x =>
                {
                    x.RevokeRefreshTokenOnSignout = true;
                });

            services.AddHttpClient();
            services.AddHealthChecks();
            services.AddMvc(x => { x.EnableEndpointRouting = false; });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app)
        {
            app.Map(new PathString(""), (applicationBuilder) =>
            {
                if (Debugger.IsAttached)
                {
                    applicationBuilder.UseDeveloperExceptionPage();
                }
                else
                {
                    applicationBuilder.UseExceptionHandler("/Home/Error");
                    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                    //applicationBuilder.UseHsts();
                }
                applicationBuilder.UseForwardedHeaders();
                applicationBuilder.UseCookiePolicy(
                            new CookiePolicyOptions
                            {
                                Secure = CookieSecurePolicy.Always
                            });

                applicationBuilder.UseStaticFiles();
                applicationBuilder.UseAuthentication();
                applicationBuilder.UseSerilogRequestLogging(options =>
                            {
                                // Customize the message template
                                options.MessageTemplate = "Handled {RequestPath}";

                                // Emit debug-level events instead of the defaults
                                options.GetLevel = (httpContext, elapsed, ex) => LogEventLevel.Verbose;

                                // Attach additional properties to the request completion event
                                options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
                                {
                                    diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
                                    diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
                                };
                            });
                applicationBuilder.UseHealthChecks("/health");
                applicationBuilder.UseMvc(routes =>
                            {
                                routes.MapRoute(
                                    name: "default",
                                    template: "{controller=Home}/{action=Index}/{id?}");
                            });
            });
        }
    }
}