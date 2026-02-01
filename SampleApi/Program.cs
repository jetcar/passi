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
using Serilog;
using Serilog.Events;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

Log.Information("Starting SampleApi application...");

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

Log.Information("OIDC Configuration - Authority: {OidcUrl}, Audience: {Audience}", oidcUrl, audience);
if (!string.IsNullOrEmpty(virtualPath))
{
    Log.Information("Virtual path configured: {VirtualPath}", virtualPath);
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
                Log.Information("Redirecting to OIDC provider");
                return Task.CompletedTask;
            },
            OnAuthenticationFailed = context =>
            {
                Log.Error("Authentication failed: {Error}", context.Exception.Message);
                return Task.CompletedTask;
            },
            OnTokenValidated = context =>
            {
                Log.Information("Token validated for user: {User}", context.Principal?.Identity?.Name ?? "Unknown");
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

Log.Information("Application built successfully");

// Configure virtual path if specified
if (!string.IsNullOrEmpty(virtualPath))
{
    app.UsePathBase(new PathString(virtualPath));
    Log.Information("Using path base: {PathBase}", virtualPath);
}

// Use Serilog request logging (following solution pattern)
app.UseSerilogRequestLogging(options =>
{
    options.MessageTemplate = "Handled {RequestPath}";
    options.GetLevel = (httpContext, elapsed, ex) => LogEventLevel.Verbose;
    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
        diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
    };
});

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

Log.Information("Application starting on port 5006");
Log.Information("Swagger UI available at /swagger");
app.Run();
