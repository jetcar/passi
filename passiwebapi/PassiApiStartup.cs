using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ConfigurationManager;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.OpenApi.Models;
using passi_webapi.Controllers;
using Repos;
using Serilog;
using Serilog.Events;
using Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http.Features.Authentication;
using System.Threading.Tasks;
using System.Security.Claims;
using OpenIdLib.OpenId;

namespace passi_webapi
{
    public class PassiApiStartup
    {
        public PassiApiStartup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
            services.AddControllers();
            services.AddSingleton<AppSetting>();
            services.AddSingleton<IEmailSender, EmailSender>();
            services.AddSingleton<IRandomGenerator, RandomGenerator>();
            services.AddSingleton<IFireBaseClient, FireBaseClient>();
            services.AddScoped<IFirebaseService, FirebaseService>();
            services.AddSingleton<ICertValidator, CertValidator>();
            services.AddScoped<PassiDbContext>();
            services.AddScoped<CurrentContext>();
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<ISessionsRepository, SessionsRepository>();
            services.AddScoped<ICertificateRepository, CertificateRepository>();
            services.AddScoped<ICertificatesService, CertificatesService>();
            services.AddSingleton<IStartupFilter, MigrationStartupFilter<PassiDbContext>>();
            services.AddSwaggerGenNewtonsoftSupport();

            services.AddDataProtection()
                .SetApplicationName("PassiApp")
                .AddKeyManagementOptions(options =>
                {
                    options.AutoGenerateKeys = true;
                    options.NewKeyLifetime = TimeSpan.FromDays(7);
                })
                .PersistKeysToDbContext<PassiDbContext>();

            var identityUrl = Environment.GetEnvironmentVariable("IdentityUrl") ?? Configuration.GetValue<string>("AppSetting:IdentityUrl");
            var returnUrl = Environment.GetEnvironmentVariable("returnUrl") ?? Configuration.GetValue<string>("AppSetting:returnUrl");
            var passiUrl = Environment.GetEnvironmentVariable("PassiUrl") ?? Configuration.GetValue<string>("AppSetting:PassiUrl");
            var clientId = Environment.GetEnvironmentVariable("PassiClientId") ?? Configuration.GetValue<string>("AppSetting:PassiClientId");
            var secret = Environment.GetEnvironmentVariable("PassiSecret") ?? Configuration.GetValue<string>("AppSetting:PassiSecret");

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

            services.AddHealthChecks();

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "My API", Version = "v1" });
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.Map(new PathString("/passiapi"), (applicationBuilder) =>
            {
                if (env.IsDevelopment())
                {
                    applicationBuilder.UseDeveloperExceptionPage();
                }

                applicationBuilder.UseRouting();

                applicationBuilder.UseMiddleware<ErrorHandlerMiddleware>();
                applicationBuilder.UseForwardedHeaders();
                applicationBuilder.UseHttpsRedirection();
                applicationBuilder.UseCookiePolicy(
                    new CookiePolicyOptions
                    {
                        Secure = CookieSecurePolicy.Always
                    });
                applicationBuilder.UseMiddleware<MyAuthenticationMiddleware>();
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
                applicationBuilder.UseEndpoints(endpoints =>
                {
                    endpoints.MapControllers();
                });

                applicationBuilder.UseHealthChecks("/health");
                applicationBuilder.UseSwagger();
                applicationBuilder.UseSwaggerUI(c => { c.SwaggerEndpoint("v1/swagger.json", "My API V1"); });
            });
        }
    }

    public class MyAuthenticationMiddleware

    {
        private readonly RequestDelegate _next;

        /// <summary>
        /// Initializes a new instance of <see cref="AuthenticationMiddleware"/>.
        /// </summary>
        /// <param name="next">The next item in the middleware pipeline.</param>
        /// <param name="schemes">The <see cref="IAuthenticationSchemeProvider"/>.</param>
        public MyAuthenticationMiddleware(RequestDelegate next, IAuthenticationSchemeProvider schemes)
        {
            if (next == null)
            {
                throw new ArgumentNullException(nameof(next));
            }
            if (schemes == null)
            {
                throw new ArgumentNullException(nameof(schemes));
            }

            _next = next;
            Schemes = schemes;
        }

        /// <summary>
        /// Gets or sets the <see cref="IAuthenticationSchemeProvider"/>.
        /// </summary>
        public IAuthenticationSchemeProvider Schemes { get; set; }

        /// <summary>
        /// Invokes the middleware performing authentication.
        /// </summary>
        /// <param name="context">The <see cref="HttpContext"/>.</param>
        public async Task Invoke(HttpContext context)
        {
            context.Features.Set<IAuthenticationFeature>(new AuthenticationFeature
            {
                OriginalPath = context.Request.Path,
                OriginalPathBase = context.Request.PathBase
            });

            // Give any IAuthenticationRequestHandler schemes a chance to handle the request
            var handlers = context.RequestServices.GetRequiredService<IAuthenticationHandlerProvider>();
            foreach (var scheme in await Schemes.GetRequestHandlerSchemesAsync())
            {
                var handler = await handlers.GetHandlerAsync(context, scheme.Name) as IAuthenticationRequestHandler;
                if (handler != null && await handler.HandleRequestAsync())
                {
                    return;
                }
            }

            var defaultAuthenticate = await Schemes.GetDefaultAuthenticateSchemeAsync();
            if (defaultAuthenticate != null)
            {
                var result = await context.AuthenticateAsync(defaultAuthenticate.Name);
                if (result?.Principal != null)
                {
                    context.User = result.Principal;
                }
                if (result?.Succeeded ?? false)
                {
                    var authFeatures = new AuthenticationFeatures(result);
                    context.Features.Set<IHttpAuthenticationFeature>(authFeatures);
                    context.Features.Set<IAuthenticateResultFeature>(authFeatures);
                }
            }

            await _next(context);
        }
    }

    internal class AuthenticationFeatures : IAuthenticateResultFeature, IHttpAuthenticationFeature
    {
        private ClaimsPrincipal? _user;
        private AuthenticateResult? _result;

        public AuthenticationFeatures(AuthenticateResult result)
        {
            AuthenticateResult = result;
        }

        public AuthenticateResult? AuthenticateResult
        {
            get => _result;
            set
            {
                _result = value;
                _user = _result?.Principal;
            }
        }

        public ClaimsPrincipal? User
        {
            get => _user;
            set
            {
                _user = value;
                _result = null;
            }
        }
    }
}