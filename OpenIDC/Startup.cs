using System;
using System.IO;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography.X509Certificates;
using ConfigurationManager;
using Google.Cloud.Diagnostics.Common;
using GoogleTracer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Quartz;
using Serilog;
using Serilog.Events;
using Services;
using static OpenIddict.Abstractions.OpenIddictConstants;


public class Startup
{
    public Startup(IConfiguration configuration)
        => Configuration = configuration;

    public IConfiguration Configuration { get; }

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddControllersWithViews();
        services.AddRazorPages();
        var identityBaseUrl = Environment.GetEnvironmentVariable("IdentityUrlBase") ?? Configuration.GetValue<string>("AppSetting:IdentityUrlBase");
        var projectId = Environment.GetEnvironmentVariable("projectId") ?? Configuration.GetValue<string>("AppSetting:projectId");
        Tracer.SetupTracer(services, projectId, "PassiOpenIdc");

        services.AddDbContext<ApplicationDbContext>();
        services.AddSingleton<IRandomGenerator, RandomGenerator>();
        services.AddSingleton<IMyRestClient, MyRestClient>();
        services.AddSingleton<AppSetting>();
        //services.AddSerilog();
        // services.AddDatabaseDeveloperPageExceptionFilter();

        // Register the Identity services.
        services.AddIdentity<IdentityUser, IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders()
            .AddDefaultUI()
            ;
        services.AddHttpClient("tracesOutgoing")
            // The next call guarantees that trace information is propagated for outgoing
            // requests that are already being traced.
            .AddOutgoingGoogleTraceHandler();
        // OpenIddict offers native integration with Quartz.NET to perform scheduled tasks
        // (like pruning orphaned authorizations/tokens from the database) at regular intervals.
        //services.AddQuartz(options =>
        //{
        //    options.UseSimpleTypeLoader();
        //    options.UseInMemoryStore();
        //});

        // Register the Quartz.NET service and configure it to block shutdown until jobs are complete.
        // services.AddQuartzHostedService(options => options.WaitForJobsToComplete = true);

        services.AddOpenIddict()

            // Register the OpenIddict core components.
            .AddCore(options =>
            {
                // Configure OpenIddict to use the Entity Framework Core stores and models.
                // Note: call ReplaceDefaultEntities() to replace the default OpenIddict entities.
                options.UseEntityFrameworkCore()
                       .UseDbContext<ApplicationDbContext>();

                // Enable Quartz.NET integration.
                //options.UseQuartz();
            })
            // Register the OpenIddict server components.
            .AddServer(options =>
            {
                // Enable the authorization, logout, token and userinfo endpoints.
                options.SetIssuer($"{identityBaseUrl}/openidc")
                    .SetAuthorizationEndpointUris($"{identityBaseUrl}/openidc/login", $"{identityBaseUrl}/openidc/api/login", $"{identityBaseUrl}/openidc/api/check")
                       .SetEndSessionEndpointUris($"{identityBaseUrl}/openidc/connect/logout")
                       .SetTokenEndpointUris($"{identityBaseUrl}/openidc/connect/token")
                       .SetUserInfoEndpointUris($"{identityBaseUrl}/openidc/connect/userinfo")
                    .SetJsonWebKeySetEndpointUris($"{identityBaseUrl}/openidc/.well-known/jwks");

                // Mark the "email", "profile" and "roles" scopes as supported scopes.
                options.RegisterScopes(Scopes.Email, Scopes.Profile, Scopes.Roles);

                // Note: this sample only uses the authorization code flow but you can enable
                // the other flows if you need to support implicit, password or client credentials.
                options.AllowAuthorizationCodeFlow();
                //options.RequireProofKeyForCodeExchange();
                // Register the signing and encryption credentials.
                //options.AddDevelopmentEncryptionCertificate();
                //options.AddDevelopmentSigningCertificate();
                options.AddSigningCertificate(X509Certificate2.CreateFromPemFile("/myapp/cert/certs/certificate.crt", "/myapp/cert/certs/privatekey.pem"));
                options.AddEncryptionCertificate(X509Certificate2.CreateFromPemFile("/myapp/cert/certs/certificate.crt", "/myapp/cert/certs/privatekey.pem"));
                // Register the ASP.NET Core host and configure the ASP.NET Core-specific options.
                options.UseAspNetCore()
                       .EnableAuthorizationEndpointPassthrough()
                       .EnableEndSessionEndpointPassthrough()
                       .EnableTokenEndpointPassthrough()
                       .EnableUserInfoEndpointPassthrough()
                       .EnableStatusCodePagesIntegration()
                       //.DisableTransportSecurityRequirement()
                       ;
            })

            // Register the OpenIddict validation components.
            .AddValidation(options =>
            {
                // Import the configuration from the local OpenIddict server instance.
                options.UseLocalServer();

                // Register the ASP.NET Core host.
                options.UseAspNetCore();
            });

        services.AddHttpContextAccessor();
        services.AddHttpClient();
        services.AddHealthChecks();

        // Register the worker responsible for seeding the database.
        // Note: in a real world application, this step should be part of a setup script.
        services.AddHostedService<Worker>();
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        app.UseForwardedHeaders(new ForwardedHeadersOptions
        {
            ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedHost | ForwardedHeaders.XForwardedProto
        });

        app.Map(new PathString("/openidc"), (appBuilder) =>
        {
            Tracer.CurrentTracer = app.ApplicationServices.GetService<IManagedTracer>();

            // app.UseHttpsRedirection();
            appBuilder.UseDefaultFiles();
            appBuilder.UseStaticFiles(new StaticFileOptions()
            {
                OnPrepareResponse = (context) =>
                {
                    // Disable caching for all static files.
                    context.Context.Response.Headers["Cache-Control"] = "no-cache, no-store";
                    context.Context.Response.Headers["Pragma"] = "no-cache";
                    context.Context.Response.Headers["Expires"] = "-1";
                }
            });
            appBuilder.UseSerilogRequestLogging(options =>
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
            appBuilder.UseRouting();


            appBuilder.UseAuthentication();
            appBuilder.UseAuthorization();

            appBuilder.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                // endpoints.MapDefaultControllerRoute();
                // endpoints.MapRazorPages();
                endpoints.MapFallbackToFile("/index.html");

            });
        });
    }
}
