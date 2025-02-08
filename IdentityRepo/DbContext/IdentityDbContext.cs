using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ConfigurationManager;
using MessagePack;
using MessagePack.Formatters;
using MessagePack.Resolvers;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenIddict.Abstractions;
using Serilog.Core;
using ILogger = Serilog.ILogger;

namespace IdentityRepo.DbContext
{
    [GoogleTracer.Profile]
    public class IdentityDbContext : IdentityDbContext<IdentityUser>, IDataProtectionKeyContext
    {
        private readonly AppSetting _appSetting;
        private ILogger _logger;

        //public IdentityDbContext()
        //{
        //    var myConfiguration = new Dictionary<string, string>
        //    {
        //        {"AppSetting:IdentityDbName", "Identity"},
        //        {"AppSetting:DbUser", "postgres"},
        //        {"AppSetting:DbPassword", "q"},
        //        {"AppSetting:DbHost", "localhost"},
        //    };
        //    var config = new ConfigurationBuilder().AddInMemoryCollection(myConfiguration).Build();
        //    var appSetting = new AppSetting(config);
        //    appSetting.PrefferAppsettingFile = true;
        //    _logger = Logger.None;
        //    _appSetting = appSetting;
        //}
        public IdentityDbContext(AppSetting appSetting, ILogger logger)
        {
            _appSetting = appSetting;
            _logger = logger;
        }

        //public IdentityDbContext(ILogger logger) : base(new DbContextOptions<IdentityDbContext>())
        //{
        //    AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
        //    _logger = logger;
        //    var myConfiguration = new Dictionary<string, string>
        //    {
        //        {"AppSetting:IdentityDbName", "Identity"},
        //        {"AppSetting:DbUser", "postgres"},
        //        {"AppSetting:DbPassword", "q"},
        //        {"AppSetting:DbHost", "localhost"},
        //        {"AppSetting:DbPort", "5432"},
        //    };
        //    var config = new ConfigurationBuilder().AddInMemoryCollection(myConfiguration).Build();
        //    var appSetting = new AppSetting(config);
        //    appSetting.PrefferAppsettingFile = true;
        //    _appSetting = appSetting;
        //}

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            //optionsBuilder.UseModel(IdentityDbContextModel.Instance);
            _logger.Debug("dbhost=" + _appSetting["DbHost"]);
            var trustMode = _appSetting["DbSslMode"] == "Require" ? "Trust Server Certificate=true;" : "";
            //optionsBuilder.AddInterceptors(new TaggedQueryCommandInterceptor(_logger));
            var connectionString = $"host={_appSetting["DbHost"]};port={_appSetting["DbPort"]};database={_appSetting["IdentityDbName"]};user id={_appSetting["DbUser"]};password={_appSetting["DbPassword"]};Ssl Mode={_appSetting["DbSslMode"]};{trustMode}";
            Console.WriteLine(connectionString);
            optionsBuilder.UseOpenIddict();
            optionsBuilder.UseNpgsql(connectionString, o =>
            {
                o.EnableRetryOnFailure(30, TimeSpan.FromSeconds(2), null);
            });
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.UseOpenIddict();

            modelBuilder.Entity<UserClient>(entity =>
            {
                entity.HasKey(c => new { c.UserId, ClientId_new = c.ClientId_new });


            });

        }
        public DbSet<UserClient> UserClients { get; set; }
        public DbSet<DataProtectionKey> DataProtectionKeys { get; set; }
        public Task<int> SaveChangesAsync()
        {
            return base.SaveChangesAsync();
        }
    }



    public class UserClient
    {
        public string UserId { get; set; }
        public string ClientId_new { get; set; }
        public string ClientSecret { get; set; }
    }
}