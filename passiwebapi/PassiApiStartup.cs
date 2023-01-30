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
using System.Linq;
using Models;
using AutoMapper;
using passi_webapi.Dto;
using NodaTime;

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
            var config = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<UserDb, UserDto>();
                cfg.CreateMap<DeviceDb, DeviceDto>();
                cfg.CreateMap<CertificateDb, CertificateDto>();
                cfg.CreateMap<UserInvitationDb, UserInvitationDto>();
                cfg.CreateMap<SessionDb, SessionDto>();
                cfg.CreateMap<Instant, DateTime>().ConvertUsing(s => s.ToDateTimeUtc());
            });
            var mapper = config.CreateMapper();

            var identityUrl = Environment.GetEnvironmentVariable("IdentityUrl") ?? Configuration.GetValue<string>("AppSetting:IdentityUrl");
            var returnUrl = Environment.GetEnvironmentVariable("returnUrl") ?? Configuration.GetValue<string>("AppSetting:returnUrl");
            var passiUrl = Environment.GetEnvironmentVariable("PassiUrl") ?? Configuration.GetValue<string>("AppSetting:PassiUrl");
            var clientId = Environment.GetEnvironmentVariable("PassiClientId") ?? Configuration.GetValue<string>("AppSetting:PassiClientId");
            var secret = Environment.GetEnvironmentVariable("PassiSecret") ?? Configuration.GetValue<string>("AppSetting:PassiSecret");


            services.AddControllers();
            services.AddSingleton<AppSetting>();
            services.AddSingleton(mapper);
            var myRestClient = new MyRestClient(passiUrl);
            services.AddSingleton<IMyRestClient>(myRestClient);

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
            .AddOpenIdAuthentication(identityUrl, returnUrl, passiUrl, clientId, secret,myRestClient)
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
            InitializeDatabase(app);
            app.Map(new PathString("/passiapi"), (applicationBuilder) =>
            {
                if (env.IsDevelopment())
                {
                    applicationBuilder.UseDeveloperExceptionPage();
                }

                applicationBuilder.UseRouting();

                //applicationBuilder.UseMiddleware<MyAuthenticationMiddleware>();
                applicationBuilder.UseMiddleware<ErrorHandlerMiddleware>();
                applicationBuilder.UseForwardedHeaders();
                applicationBuilder.UseHttpsRedirection();
                applicationBuilder.UseAuthentication();
                applicationBuilder.UseAuthorization();
                applicationBuilder.UseCookiePolicy(
                    new CookiePolicyOptions
                    {
                        Secure = CookieSecurePolicy.Always
                    });
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

        private void InitializeDatabase(IApplicationBuilder app)
        {
            using (var serviceScope = app.ApplicationServices.GetService<IServiceScopeFactory>().CreateScope())
            {
                var appsettings = serviceScope.ServiceProvider.GetRequiredService<AppSetting>();
                var context = serviceScope.ServiceProvider.GetRequiredService<PassiDbContext>();

                if (!context.Admins.Any(x => x.Email == "admin@passi.cloud"))
                {
                    context.Admins.Add(new AdminDb() { Email = "admin@passi.cloud" });
                    context.SaveChanges();
                }
            }
        }
    }
}