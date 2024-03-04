using ConfigurationManager;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System;
using System.Collections.Generic;

namespace WebApp
{
    public class WebAppDbContext : DbContext, IDataProtectionKeyContext
    {
        private ILogger _logger;
        private AppSetting _appSetting;
        private string _connectionString;

        public WebAppDbContext()
        {
            //_logger = logger;
            var myConfiguration = new Dictionary<string, string>
            {
                {"AppSetting:DbName", "WebApp"},
                {"AppSetting:DbUser", "postgres"},
                {"AppSetting:DbPassword", "q"},
                {"AppSetting:DbHost", "localhost"},
                {"AppSetting:DbPort", "5432"},
            };
            var config = new ConfigurationBuilder().AddInMemoryCollection(myConfiguration).Build();
            var appSetting = new AppSetting(config);
            appSetting.PrefferAppsettingFile = true;
            _appSetting = appSetting;
        }

        public WebAppDbContext(AppSetting appSetting, ILogger logger)
        {
            _appSetting = appSetting;
            _logger = logger;
        }

        public DbSet<DataProtectionKey> DataProtectionKeys { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            var trustMode = _appSetting["DbSslMode"] == "Require" ? "Trust Server Certificate=true;" : "";
            //optionsBuilder.AddInterceptors(new TaggedQueryCommandInterceptor(_logger));
            _connectionString = $"host={_appSetting["DbHost"]};port={_appSetting["DbPort"]};database={_appSetting["WebAppDbName"]};user id={_appSetting["DbUser"]};password={_appSetting["DbPassword"]};Ssl Mode={_appSetting["DbSslMode"]};{trustMode}";

            var serviceProvider = new ServiceCollection()
                .AddEntityFrameworkNpgsql()
                .BuildServiceProvider();
            optionsBuilder.UseNpgsql(_connectionString, c =>
            {
                c.MigrationsAssembly(typeof(WebAppDbContext).Assembly.FullName);
                c.EnableRetryOnFailure(30, TimeSpan.FromSeconds(2), null);
            }).UseInternalServiceProvider(serviceProvider);
        }
    }
}