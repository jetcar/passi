using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using ConfigurationManager;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using log4net;
using Services;

var builder = WebApplication.CreateBuilder(args);

// Configure Log4net
var logRepository = LogManager.GetRepository(System.Reflection.Assembly.GetEntryAssembly());
var configFile = new System.IO.FileInfo("../log4net.config");
if (configFile.Exists)
{
    log4net.Config.XmlConfigurator.Configure(logRepository, configFile);
}

var log = LogManager.GetLogger(typeof(Program));
log.Info("Starting SampleApi application...");

builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(5006);
});

// Add services to the container
builder.Services.AddSingleton<AppSetting>();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient();
builder.Services.AddHealthChecks();

// Configure OIDC authentication using AppSetting pattern (env var takes precedence over config)
var configuration = builder.Configuration;
var oidcUrl = Environment.GetEnvironmentVariable("openIdcUrl")
    ?? configuration["AppSetting:openIdcUrl"]
    ?? "https://localhost/openidc";
var audience = Environment.GetEnvironmentVariable("ApiAudience")
    ?? configuration["AppSetting:ApiAudience"]
    ?? "sample-api";
var virtualPath = Environment.GetEnvironmentVariable("VirtualPath")
    ?? configuration["AppSetting:VirtualPath"]
    ?? "";

log.Info($"OIDC Configuration - Authority: {oidcUrl}, Audience: {audience}");
if (!string.IsNullOrEmpty(virtualPath))
{
    log.Info($"Virtual path configured: {virtualPath}");
}

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
    })
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.LogoutPath = "/logout";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(50);
        options.SlidingExpiration = false;
    })
    .AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, options =>
    {
        options.Authority = oidcUrl;
        options.ClientId = audience;
        options.ClientSecret = Environment.GetEnvironmentVariable("ClientSecret")
            ?? configuration["AppSetting:ClientSecret"]
            ?? "secret";
        options.ResponseType = OpenIdConnectResponseType.Code;
        options.Scope.Clear();
        options.Scope.Add("openid");
        options.Scope.Add("profile");
        options.Scope.Add("email");
        options.GetClaimsFromUserInfoEndpoint = true;
        options.SaveTokens = true;
        options.RequireHttpsMetadata = false;
        options.CallbackPath = new PathString("/callback");
        options.SignedOutCallbackPath = new PathString("/signout-callback");

        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        };
        options.BackchannelHttpHandler = handler;

        options.Events = new OpenIdConnectEvents
        {
            OnRedirectToIdentityProvider = context =>
            {
                log.Info("Redirecting to OIDC provider");
                return Task.CompletedTask;
            },
            OnAuthenticationFailed = context =>
            {
                log.Error("Authentication failed", context.Exception);
                return Task.CompletedTask;
            },
            OnTokenValidated = context =>
            {
                log.Info($"Token validated for user: {context.Principal?.Identity?.Name ?? "Unknown"}");
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

log.Info("Application built successfully");

// Configure virtual path if specified
if (!string.IsNullOrEmpty(virtualPath))
{
    app.UsePathBase(new PathString(virtualPath));
    log.Info($"Using path base: {virtualPath}");
}

// Use correlation ID middleware for request tracking
app.UseCorrelationId();

// Configure the HTTP request pipeline
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Sample API v1");
    c.RoutePrefix = "swagger";
});

app.UseAuthentication();
app.UseAuthorization();

app.UseHealthChecks("/health");

app.MapGet("/", () => "Sample API - Protected by OpenIDC");

app.MapGet("/login", () => Results.Challenge(new AuthenticationProperties
{
    RedirectUri = "/"
},
new[] { OpenIdConnectDefaults.AuthenticationScheme }))
    .WithName("Login")
    .WithOpenApi();

app.MapGet("/logout", async (HttpContext context) =>
{
    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    await context.SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme);
    return Results.Redirect("/");
})
    .WithName("Logout")
    .WithOpenApi();

app.MapGet("/callback", () => Results.Redirect("/"))
    .WithName("Callback")
    .WithOpenApi();

app.MapGet("/api/public", () => new { message = "This is a public endpoint" })
    .WithName("PublicEndpoint")
    .WithOpenApi();

app.MapGet("/api/protected", [Authorize] () => new { message = "This is a protected endpoint", user = "authenticated" })
    .WithName("ProtectedEndpoint")
    .WithOpenApi();

app.MapGet("/api/user", [Authorize] (HttpContext context) =>
{
    var claims = context.User.Claims.Select(c => new { c.Type, c.Value });
    return new { user = context.User.Identity?.Name, claims };
})
    .WithName("UserInfo")
    .WithOpenApi();

log.Info("Application starting on port 5006");
log.Info("Swagger UI available at /swagger");
app.Run();
