using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using ConfigurationManager;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using ModelContextProtocol.AspNetCore.Authentication;
using ModelContextProtocol.Authentication;
using ModelContextProtocol.Protocol;

namespace WebApp.News
{
    /// <summary>
    /// Remote MCP server at /mcp so AI agents can manage news posts. Agents sign in with Passi through OpenIDC
    /// (authorization code + PKCE); the endpoint accepts OpenIDC access tokens issued to one of the NewsMcpClientIds
    /// and only for users listed in NewsAdminEmails.
    /// </summary>
    public static class NewsMcp
    {
        public const string Path = "/mcp";
        public const string BearerScheme = "NewsMcpBearer";
        public const string Policy = "NewsMcp";

        public static IServiceCollection AddNewsMcp(this IServiceCollection services, AppSetting appSetting, HttpMessageHandler backchannel)
        {
            var identityBase = appSetting["IdentityUrlBase"]?.TrimEnd('/');
            var publicBase = (appSetting["PublicUrlBase"] ?? identityBase)?.TrimEnd('/');
            var issuer = $"{identityBase}/openidc";
            var openIdcUrl = (Environment.GetEnvironmentVariable("openIdcUrl") ?? appSetting["openIdcUrl"])?.TrimEnd('/');

            // OpenIDC sets the access token audience to the client id, so this list is what stops a token that
            // some other Passi-integrated website received from being replayed here.
            var allowedClientIds = (appSetting["NewsMcpClientIds"] ?? "")
                .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList();

            services.AddSingleton(new AuthorizationServerMetadataProxy(openIdcUrl, backchannel));

            services.AddAuthentication()
                .AddJwtBearer(BearerScheme, options =>
                {
                    options.MetadataAddress = $"{openIdcUrl}/.well-known/openid-configuration";
                    options.RequireHttpsMetadata = openIdcUrl?.StartsWith("https://", StringComparison.OrdinalIgnoreCase) == true;
                    if (backchannel != null)
                        options.BackchannelHttpHandler = backchannel;
                    options.MapInboundClaims = false;
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidIssuer = issuer,
                        ValidateAudience = true,
                        // An empty list with ValidateAudience rejects every token: MCP stays closed until configured.
                        ValidAudiences = allowedClientIds,
                        ValidateLifetime = true,
                        NameClaimType = "email",
                    };
                    options.ForwardChallenge = McpAuthenticationDefaults.AuthenticationScheme;
                })
                .AddMcp(options =>
                {
                    options.ResourceMetadata = new ProtectedResourceMetadata
                    {
                        Resource = $"{publicBase}{Path}",
                        AuthorizationServers = { issuer },
                        ScopesSupported = { "openid", "profile", "email" },
                        ResourceName = "Passi News",
                    };
                });

            services.AddAuthorization(options => options.AddPolicy(Policy, policy => policy
                .AddAuthenticationSchemes(BearerScheme)
                .RequireAuthenticatedUser()
                .RequireAssertion(context =>
                    context.Resource is HttpContext http &&
                    http.RequestServices.GetRequiredService<NewsAdmins>().IsAdmin(context.User))));

            services.AddMcpServer(options =>
                {
                    options.ServerInfo = new Implementation { Name = "passi-news", Title = "Passi News", Version = "1.0.0" };
                    options.ServerInstructions =
                        "Manage news posts shown on https://passi.cloud (home page and /news). Posts are Markdown. " +
                        "create_post makes a draft unless publish is true; published posts are public immediately.";
                })
                .WithHttpTransport(options => options.Stateless = true)
                .WithTools<NewsMcpTools>();

            return services;
        }

        public static IEndpointConventionBuilder MapNewsMcp(this IEndpointRouteBuilder endpoints) =>
            endpoints.MapMcp(Path).RequireAuthorization(Policy);

        // Where MCP clients look for authorization server metadata when they skip the protected-resource document
        // or drop the /openidc path of its authorization server (RFC 8414 root and path-insert forms, and the older
        // "relative to the MCP URL" forms). Without these a client can neither sign in nor refresh its token.
        private static readonly string[] AuthorizationServerMetadataPaths =
        {
            "/.well-known/oauth-authorization-server",
            "/.well-known/oauth-authorization-server" + Path,
            "/.well-known/openid-configuration" + Path,
            Path + "/.well-known/oauth-authorization-server",
            Path + "/.well-known/openid-configuration",
        };

        /// <summary>
        /// Serves OpenIDC's metadata at the fallback discovery paths, and a 404 (not the SPA) for any other
        /// /mcp/.well-known path.
        /// </summary>
        public static IApplicationBuilder UseNewsMcpDiscovery(this IApplicationBuilder app) =>
            app.Use(async (context, next) =>
            {
                var path = context.Request.Path;
                if (!HttpMethods.IsGet(context.Request.Method) && !HttpMethods.IsHead(context.Request.Method))
                {
                    await next();
                    return;
                }

                if (AuthorizationServerMetadataPaths.Any(p => path.Equals(p, StringComparison.OrdinalIgnoreCase)))
                {
                    var proxy = context.RequestServices.GetRequiredService<AuthorizationServerMetadataProxy>();
                    var metadata = await proxy.GetAsync(context.RequestAborted);
                    if (metadata == null)
                    {
                        context.Response.StatusCode = StatusCodes.Status502BadGateway;
                        return;
                    }
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsync(metadata, context.RequestAborted);
                    return;
                }

                if (path.StartsWithSegments(Path + "/.well-known"))
                {
                    context.Response.StatusCode = StatusCodes.Status404NotFound;
                    return;
                }

                await next();
            });
    }

    /// <summary>Fetches OpenIDC's discovery document over the internal hop.</summary>
    public class AuthorizationServerMetadataProxy
    {
        private readonly string _discoveryUrl;
        private readonly HttpClient _httpClient;

        public AuthorizationServerMetadataProxy(string openIdcUrl, HttpMessageHandler backchannel)
        {
            _discoveryUrl = $"{openIdcUrl}/.well-known/openid-configuration";
            // The backchannel handler is shared with JwtBearer, so this client must not dispose it.
            _httpClient = backchannel != null ? new HttpClient(backchannel, disposeHandler: false) : new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(10);
        }

        /// <returns>The metadata JSON, or null when OpenIDC can't be reached or answers with an error.</returns>
        public async Task<string> GetAsync(System.Threading.CancellationToken cancellationToken)
        {
            try
            {
                using var response = await _httpClient.GetAsync(_discoveryUrl, cancellationToken);
                return response.IsSuccessStatusCode ? await response.Content.ReadAsStringAsync(cancellationToken) : null;
            }
            catch (Exception e) when (e is HttpRequestException || e is TaskCanceledException)
            {
                return null;
            }
        }
    }
}
