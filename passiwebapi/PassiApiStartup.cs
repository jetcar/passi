using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ConfigurationManager;
using Microsoft.AspNetCore.Http;
using Microsoft.OpenApi.Models;
using passi_webapi.Controllers;
using Repos;
using Serilog;
using Serilog.Events;
using Services;

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

                applicationBuilder.UseMiddleware<ErrorHandlerMiddleware>();
                applicationBuilder.UseRouting();

                applicationBuilder.UseAuthorization();
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

                applicationBuilder.UseSwagger();
                applicationBuilder.UseSwaggerUI(c => { c.SwaggerEndpoint("v1/swagger.json", "My API V1"); });
            });
        }
    }
}