using ConfigurationManager;
using GoogleTracer;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using Serilog.Events;
using Services;
using System;
using System.Net.Http;
using Google.Cloud.Diagnostics.Common;
using Serilog;
using WebApp.Services;

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
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
            };
            var passiUrl = Environment.GetEnvironmentVariable("PassiUrl") ?? Configuration.GetValue<string>("AppSetting:PassiUrl");
            var identityUrl = Environment.GetEnvironmentVariable("openIdcUrl") ?? Configuration.GetValue<string>("AppSetting:openIdcUrl");
            var returnUrl = Environment.GetEnvironmentVariable("returnUrl") ?? Configuration.GetValue<string>("AppSetting:returnUrl");
            var clientId = Environment.GetEnvironmentVariable("ClientId") ?? Configuration.GetValue<string>("AppSetting:ClientId");
            var secret = Environment.GetEnvironmentVariable("ClientSecret") ?? Configuration.GetValue<string>("AppSetting:ClientSecret");
            var projectId = Environment.GetEnvironmentVariable("projectId") ?? Configuration.GetValue<string>("AppSetting:projectId");

            Tracer.SetupTracer(services, projectId, "PassiWebApp");

            services.AddSingleton<AppSetting>();
            services.AddScoped<WebAppDbContext>();
            services.AddSingleton<IMyRestClient, MyRestClient>();
            services.AddSingleton<IOidcClient, OidcClient>();

            // Use PostgreSQL only for long-term static data (DataProtectionKeys)
            services.AddDataProtection()
                .SetApplicationName("WebApp")
                .AddKeyManagementOptions(options =>
                {
                    options.AutoGenerateKeys = true;
                    options.NewKeyLifetime = TimeSpan.FromDays(7);
                })
                .PersistKeysToDbContext<WebAppDbContext>();

            // Register an HttpClient for outgoing requests.
            services.AddHttpClient("tracesOutgoing")
                // The next call guarantees that trace information is propagated for outgoing
                // requests that are already being traced.
                .AddOutgoingGoogleTraceHandler();

            services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(10);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            });

            services.AddAuthentication(options =>
                {
                    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                })
              .AddCookie(options =>
                {
                    options.LoginPath = "/login";
                    options.LogoutPath = "/logout";
                    options.ExpireTimeSpan = TimeSpan.FromMinutes(50);
                    options.SlidingExpiration = false;
                });


            services.AddHttpContextAccessor();
            services.AddHttpClient();
            services.AddHealthChecks();
            services.AddMvc(x => { x.EnableEndpointRouting = false; });

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "My API", Version = "v1" });
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app)
        {
            app.Map(new PathString(""), (applicationBuilder) =>
            {
                Tracer.CurrentTracer = app.ApplicationServices.GetService<IManagedTracer>();

                applicationBuilder.UseRouting();

                applicationBuilder.UseForwardedHeaders();
                applicationBuilder.UseCookiePolicy(
                            new CookiePolicyOptions
                            {
                                Secure = CookieSecurePolicy.Always
                            });

                applicationBuilder.UseSession();
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

                applicationBuilder.UseAuthentication();
                applicationBuilder.UseAuthorization();
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
    }
}