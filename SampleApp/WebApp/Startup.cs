using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using ConfigurationManager;
using Microsoft.AspNetCore.DataProtection;
using Serilog;
using Serilog.Events;
using WebApp.OpenId;

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
            /*services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedForHeaderName = "X-Forwarded-Host";
            });*/
            //services.Configure<CookiePolicyOptions>(options =>
            //{
            //    // This lambda determines whether user consent for non-essential cookies is needed for a given request.
            //    options.CheckConsentNeeded = context => false;
            //    options.MinimumSameSitePolicy = SameSiteMode.None;
            //});

            services.AddSingleton<AppSetting>();
            services.AddScoped<WebAppDbContext>();
            services.AddSingleton<IStartupFilter, MigrationStartupFilter<WebAppDbContext>>();

            services.AddDataProtection()
                .SetApplicationName("WebApp")
                .AddKeyManagementOptions(options =>
                {
                    options.AutoGenerateKeys = true;
                    options.NewKeyLifetime = TimeSpan.FromDays(7);
                })
                .PersistKeysToDbContext<WebAppDbContext>();

            var identityUrl = Environment.GetEnvironmentVariable("IdentityUrl") ?? Configuration.GetValue<string>("AppSetting:IdentityUrl");
            var returnUrl = Environment.GetEnvironmentVariable("returnUrl") ?? Configuration.GetValue<string>("AppSetting:returnUrl");
            var passiUrl = Environment.GetEnvironmentVariable("PassiUrl") ?? Configuration.GetValue<string>("AppSetting:PassiUrl");
            var clientId = Environment.GetEnvironmentVariable("ClientId") ?? Configuration.GetValue<string>("AppSetting:ClientId");
            var secret = Environment.GetEnvironmentVariable("ClientSecret") ?? Configuration.GetValue<string>("AppSetting:ClientSecret");
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
                .AddOpenIdAuthentication(identityUrl, returnUrl, passiUrl, clientId, secret)
                .AddOpenIdTokenManagement(x =>
                {
                    x.RevokeRefreshTokenOnSignout = true;
                });

            services.AddHttpClient();
            services.AddHealthChecks();
            services.AddMvc(x =>
           {
               x.EnableEndpointRouting = false;
           }).SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                //app.UseHsts();
            }
            app.UseForwardedHeaders();
            app.UseHttpsRedirection();
            app.UseCookiePolicy(
            new CookiePolicyOptions
            {
                Secure = CookieSecurePolicy.Always
            });

            app.UseStaticFiles();
            app.UseAuthentication();
            app.UseSerilogRequestLogging(options =>
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
            app.UseHealthChecks("/health");
            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}