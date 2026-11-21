using System;
using System.Linq;
using System.Threading.RateLimiting;
using System.Threading.Tasks;
using ConfigurationManager;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace passi_webapi.SmartHome;

public static class SmartHomeRegistration
{
    public const string OwnerScheme = "HomeOwnerBearer";
    public const string OwnerPolicy = "HomeOwner";
    public const string AgentScheme = "HomeAgent";
    public const string OwnerClaim = "passi:home-owner";

    public static IApplicationBuilder UseSmartHomeBoundary(this IApplicationBuilder app) => app.Use(async (context, next) =>
    {
        if (context.Request.Path.StartsWithSegments("/api/smarthome"))
        {
            context.Response.Headers.CacheControl = "no-store";
            if (!context.Request.IsHttps || context.Request.Query.ContainsKey("access_token"))
            {
                context.Response.StatusCode = 400;
                return;
            }
        }
        await next();
    });

    public static IServiceCollection AddSmartHome(this IServiceCollection services, AppSetting settings)
    {
        services.AddScoped<SmartHomeService>();
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<IAuthorizationHandler, HomeOwnerHandler>();
        services.AddSignalR(options => {
            options.MaximumReceiveMessageSize = 64 * 1024;
            options.MaximumParallelInvocationsPerClient = 1;
            options.EnableDetailedErrors = false;
            options.ClientTimeoutInterval = TimeSpan.FromSeconds(20);
            options.KeepAliveInterval = TimeSpan.FromSeconds(5);
        });
        var issuer = (settings["IdentityUrlBase"] ?? "").TrimEnd('/') + "/openidc";
        var audience = settings["ClientId"] ?? "";
        // Existing OpenIDC metadata/JWKS and WebApp access token; strict TLS validation is retained.
        // https://learn.microsoft.com/en-us/aspnet/core/security/authentication/configure-jwt-bearer-authentication
        services.AddAuthentication()
            .AddJwtBearer(OwnerScheme, options => {
                options.Authority = issuer;
                options.RequireHttpsMetadata = true;
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters {
                    ValidateIssuer = true, ValidIssuer = issuer,
                    ValidateAudience = true, ValidAudience = audience,
                    ValidateLifetime = true, RequireExpirationTime = true,
                    ValidateIssuerSigningKey = true, RequireSignedTokens = true,
                    ValidAlgorithms = new[] { SecurityAlgorithms.RsaSha256 },
                    ClockSkew = TimeSpan.FromSeconds(10)
                };
                options.Events = new Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerEvents {
                    OnTokenValidated = context => {
                        // ID tokens do not have client_id/scope. Reject tokens for other Passi clients.
                        if (string.IsNullOrWhiteSpace(audience) ||
                            context.Principal.FindFirst("client_id")?.Value != audience ||
                            !(context.Principal.FindFirst("scope")?.Value.Split(' ').Contains("openid") ?? false))
                            context.Fail("Access token required");
                        return Task.CompletedTask;
                    }
                };
            })
            .AddScheme<AuthenticationSchemeOptions, HomeAgentAuthenticationHandler>(AgentScheme, _ => { });
        services.AddAuthorization(options => options.AddPolicy(OwnerPolicy, policy => policy
            .AddAuthenticationSchemes(OwnerScheme).RequireAuthenticatedUser().AddRequirements(new HomeOwnerRequirement())));
        services.AddRateLimiter(options => {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy("HomeEnrollment", context => RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? "unknown", _ => new FixedWindowRateLimiterOptions {
                    PermitLimit = 10, Window = TimeSpan.FromMinutes(1), QueueLimit = 0
                }));
        });
        return services;
    }
}
