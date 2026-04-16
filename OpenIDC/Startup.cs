using System;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using ConfigurationManager;
using Google.Cloud.Diagnostics.Common;
using GoogleTracer;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using NLog;
using Services;
using RedisClient;
using OpenIDC.Services;
using OpenIDC.Middleware;


public class Startup
{
    public Startup(IConfiguration configuration)
        => Configuration = configuration;

    public IConfiguration Configuration { get; }

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddAntiforgery();
        services.AddControllersWithViews(options =>
        {
            options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
        });
        services.AddRazorPages();
        var identityBaseUrl = Environment.GetEnvironmentVariable("IdentityUrlBase") ?? Configuration.GetValue<string>("AppSetting:IdentityUrlBase");
        var projectId = Environment.GetEnvironmentVariable("projectId") ?? Configuration.GetValue<string>("AppSetting:projectId");
        Tracer.SetupTracer(services, projectId, "PassiOpenIdc");

        services.AddSingleton<IRandomGenerator, RandomGenerator>();
        services.AddSingleton<IMyRestClient, MyRestClient>();
        services.AddSingleton<AppSetting>();
        services.AddSingleton<IRedisService, RedisService>();

        // Register custom OIDC services
        services.AddSingleton<IClientStore, ClientStore>();
        services.AddSingleton<IAuthorizationCodeStore, AuthorizationCodeStore>();
        services.AddSingleton<IRefreshTokenStore, RefreshTokenStore>();

        // Configure signing credentials
        var certPath = "/myapp/cert/certs/certificate.crt";
        var keyPath = "/myapp/cert/certs/privatekey.pem";

        SigningCredentials signingCredentials;
        var log = LogManager.GetCurrentClassLogger();

        if (File.Exists(certPath) && File.Exists(keyPath))
        {
            // Load certificate from PEM files
            var cert = X509Certificate2.CreateFromPemFile(certPath, keyPath);
            var securityKey = new X509SecurityKey(cert)
            {
                KeyId = "default-key-" + DateTime.UtcNow.ToString("yyyyMMdd")
            };
            signingCredentials = new SigningCredentials(securityKey, SecurityAlgorithms.RsaSha256);
            log.Info("Loaded signing credentials from X.509 certificate");
        }
        else
        {
            // Fallback: Generate RSA key for development
            log.Warn("Certificate files not found, generating RSA key for signing (not recommended for production)");
            var rsa = RSA.Create(2048);
            var rsaKey = new RsaSecurityKey(rsa)
            {
                KeyId = "generated-key-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss")
            };
            signingCredentials = new SigningCredentials(rsaKey, SecurityAlgorithms.RsaSha256);
        }

        log.Info("Signing credentials configured successfully");

        services.AddSingleton(signingCredentials);
        services.AddSingleton<ITokenService, TokenService>();

        services.AddHttpClient("tracesOutgoing")
            .AddOutgoingGoogleTraceHandler();

        services.AddHttpContextAccessor();
        services.AddHttpClient();
        services.AddHealthChecks();

        // Add Bearer authentication for userinfo endpoint
        var signingKey = signingCredentials.Key;
        services.AddAuthentication("Bearer")
            .AddJwtBearer("Bearer", options =>
            {
                options.Authority = identityBaseUrl + "/openidc";
                options.RequireHttpsMetadata = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateAudience = false,
                    ValidateIssuer = true,
                    ValidIssuer = identityBaseUrl + "/openidc",
                    IssuerSigningKey = signingKey,
                    ValidateIssuerSigningKey = true
                };
            });

        services.AddAuthorization();

        // Add CORS policy to allow credentials for cross-origin OIDC flows
        services.AddCors(options =>
        {
            options.AddPolicy("AllowOIDCClients", builder =>
            {
                builder
                    .AllowAnyOrigin()  // In production, specify exact origins from client configuration
                    .AllowAnyMethod()
                    .AllowAnyHeader();
                // Note: When using AllowAnyOrigin(), AllowCredentials() cannot be used together
                // If you need credentials (cookies), you must specify exact origins instead
            });
        });

        // Register the worker responsible for seeding clients
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

            appBuilder.UseMiddleware<RequestLoggingMiddleware>();
            appBuilder.UseCorrelationId(); // Add correlation ID tracking for all requests

            // Serve static files BEFORE routing (for CSS, JS, images, etc.)
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

            appBuilder.UseRouting();

            // Enable CORS before authentication/authorization
            appBuilder.UseCors("AllowOIDCClients");

            appBuilder.UseAuthentication();
            appBuilder.UseAuthorization();

            appBuilder.UseEndpoints(endpoints =>
            {
                // Map API controllers (will match routes like /api/*, /connect/*, /.well-known/*)
                endpoints.MapControllers();
            });

            // SPA fallback - serve index.html for non-API routes
            appBuilder.Run(async context =>
            {
                var path = context.Request.Path;
                // Don't serve index.html for API/controller routes
                if (!path.StartsWithSegments("/api") &&
                    !path.StartsWithSegments("/connect") &&
                    !path.StartsWithSegments("/.well-known") &&
                    !System.IO.Path.HasExtension(path.Value))
                {
                    context.Request.Path = "/index.html";
                    await context.Response.SendFileAsync("wwwroot/index.html");
                }
            });
        });
    }
}
