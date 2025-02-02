using AutoMapper;
using ConfigurationManager;
using Google.Cloud.Diagnostics.AspNetCore3;
using Google.Cloud.Diagnostics.Common;
using GoogleTracer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Models;
using NodaTime;
using Repos;
using Serilog;
using Serilog.Events;
using Services;
using System;
using System.Linq;
using NotificationsService;
using RedisClient;
using WebApiDto.Auth.Dto;

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
                cfg.CreateMap<SessionTempRecord, SessionDto>();
                cfg.CreateMap<Instant, DateTime>().ConvertUsing(s => s.ToDateTimeUtc());
            });
            var mapper = config.CreateMapper();

            var identityUrl = Environment.GetEnvironmentVariable("IdentityUrl") ?? Configuration.GetValue<string>("AppSetting:IdentityUrl");
            var returnUrl = Environment.GetEnvironmentVariable("returnUrl") ?? Configuration.GetValue<string>("AppSetting:returnUrl");
            var passiUrl = Environment.GetEnvironmentVariable("PassiUrl") ?? Configuration.GetValue<string>("AppSetting:PassiUrl");
            var clientId = Environment.GetEnvironmentVariable("PassiClientId") ?? Configuration.GetValue<string>("AppSetting:PassiClientId");
            var secret = Environment.GetEnvironmentVariable("PassiSecret") ?? Configuration.GetValue<string>("AppSetting:PassiSecret");
            var projectId = Environment.GetEnvironmentVariable("projectId") ?? Configuration.GetValue<string>("AppSetting:projectId");

            Tracer.SetupTracer(services, projectId, "PassiApi");

            services.AddControllers();
            services.AddSingleton<AppSetting>(new AppSetting(Configuration));
            services.AddSingleton(mapper);
            services.AddSingleton<IMyRestClient, MyRestClient>();

            services.AddSingleton<IRedisService, RedisService>();
            services.AddSingleton<IEmailSender, SmtpEmailSender>();
            services.AddSingleton<IRandomGenerator, RandomGenerator>();
            services.AddSingleton<ICertValidator, CertValidator>();
            services.AddSingleton<IFirebaseService, FirebaseService>();
            services.AddSingleton<IFireBaseClient, FireBaseClient>();
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
            .AddOpenIdConnect(options =>
            {
                options.Authority = identityUrl; // Replace with your OpenIddict server URL
                options.ClientId = clientId;
                options.ClientSecret = secret;
                options.ResponseType = "code"; // Authorization Code Flow
                options.SaveTokens = true;
                options.Scope.Add("openid");
                options.Scope.Add("profile");
            });
            services.AddHttpContextAccessor();
            services.AddHealthChecks();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "My API", Version = "v1" });
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            InitializeDatabase(app.ApplicationServices);
            app.Map(new PathString("/passiapi"), (applicationBuilder) =>
            {
                Tracer.CurrentTracer = app.ApplicationServices.GetService<IManagedTracer>();
                if (env.IsDevelopment())
                {
                    applicationBuilder.UseDeveloperExceptionPage();
                }

                applicationBuilder.UseRouting();

                //applicationBuilder.UseMiddleware<MyAuthenticationMiddleware>();
                applicationBuilder.UseMiddleware<ErrorHandlerMiddleware>();
                applicationBuilder.UseForwardedHeaders();
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

        public void InitializeDatabase(IServiceProvider services)
        {
            using (var serviceScope = services.GetService<IServiceScopeFactory>().CreateScope())
            {
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