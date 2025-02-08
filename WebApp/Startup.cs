using ConfigurationManager;
using Google.Cloud.Diagnostics.Common;
using GoogleTracer;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.OpenApi.Models;
using Serilog;
using Serilog.Events;
using Services;
using System;
using System.Net.Http;
using OpenIddict.Abstractions;
using OpenIddict.Client;
using OpenIdLib.OpenId;
using TraceServiceOptions = Google.Cloud.Diagnostics.Common.TraceServiceOptions;

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
            var identityUrl = Environment.GetEnvironmentVariable("IdentityUrl") ?? Configuration.GetValue<string>("AppSetting:IdentityUrl");
            var returnUrl = Environment.GetEnvironmentVariable("returnUrl") ?? Configuration.GetValue<string>("AppSetting:returnUrl");
            var clientId = Environment.GetEnvironmentVariable("ClientId") ?? Configuration.GetValue<string>("AppSetting:ClientId");
            var secret = Environment.GetEnvironmentVariable("ClientSecret") ?? Configuration.GetValue<string>("AppSetting:ClientSecret");
            var projectId = Environment.GetEnvironmentVariable("projectId") ?? Configuration.GetValue<string>("AppSetting:projectId");

            Tracer.SetupTracer(services, projectId, "PassiWebApp");

            services.AddSingleton<AppSetting>();
            services.AddScoped<WebAppDbContext>();
            services.AddSingleton<IStartupFilter, MigrationStartupFilter<WebAppDbContext>>();
            services.AddSingleton<IMyRestClient, MyRestClient>();
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
                
                ;
           services.AddOpenIddict()
                
            // Register the OpenIddict core components.
            .AddCore(options =>
            {
                // Configure OpenIddict to use the Entity Framework Core stores and models.
                // Note: call ReplaceDefaultEntities() to replace the default OpenIddict entities.
                options.UseEntityFrameworkCore()
                       .UseDbContext<WebAppDbContext>();

                // Developers who prefer using MongoDB can remove the previous lines
                // and configure OpenIddict to use the specified MongoDB database:
                // options.UseMongoDb()
                //        .UseDatabase(new MongoClient().GetDatabase("openiddict"));

                // Enable Quartz.NET integration.
            }).AddClient(options =>
            {
                // Note: this sample uses the code flow, but you can enable the other flows if necessary.
                options.AllowAuthorizationCodeFlow();

                // Register the signing and encryption credentials used to protect
                // sensitive data like the state tokens produced by OpenIddict.
                options.AddDevelopmentEncryptionCertificate()
                       .AddDevelopmentSigningCertificate();
                // Register the ASP.NET Core host and configure the ASP.NET Core-specific options.
                options.UseAspNetCore()
                    .DisableTransportSecurityRequirement()
                       .EnableStatusCodePagesIntegration()
                       .EnableRedirectionEndpointPassthrough()
                       .EnablePostLogoutRedirectionEndpointPassthrough();
                // Register the System.Net.Http integration and use the identity of the current
                // assembly as a more specific user agent, which can be useful when dealing with
                // providers that use the user agent as a way to throttle requests (e.g Reddit).
                options.UseSystemNetHttp()
                    
                       .SetProductInformation(typeof(Startup).Assembly);
                options.UseSystemNetHttp(x =>
                {
                    x.ConfigureHttpClientHandler(a =>
                        a.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true);
                });



                // Add a client registration matching the client application definition in the server project.
                options.AddRegistration(new OpenIddictClientRegistration
                {
                    Issuer = new Uri(identityUrl, UriKind.Absolute),

                    ClientId = clientId,
                    ClientSecret = secret,
                    Scopes = { OpenIddictConstants.Permissions.Scopes.Email, OpenIddictConstants.Permissions.Scopes.Profile },

                    // Note: to mitigate mix-up attacks, it's recommended to use a unique redirection endpoint
                    // URI per provider, unless all the registered providers support returning a special "iss"
                    // parameter containing their URL as part of authorization responses. For more information,
                    // see https://datatracker.ietf.org/doc/html/draft-ietf-oauth-security-topics#section-4.4.
                    RedirectUri = new Uri("callback/login/local", UriKind.Relative),
                    PostLogoutRedirectUri = new Uri("callback/logout/local", UriKind.Relative)
                });
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