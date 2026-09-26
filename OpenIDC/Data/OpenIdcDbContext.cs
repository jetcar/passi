using System;
using System.Collections.Generic;
using ConfigurationManager;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using OpenIDC.Models;

namespace OpenIDC.Data
{
    public class OpenIdcDbContext : DbContext
    {
        private readonly AppSetting _appSetting;

        public OpenIdcDbContext(AppSetting appSetting)
        {
            _appSetting = appSetting;
        }

        public DbSet<RegisteredClient> RegisteredClients { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            var trustMode = _appSetting["DbSslMode"] == "Require" ? "Trust Server Certificate=true;" : "";
            var connectionString = $"host={_appSetting["DbHost"]};port={_appSetting["DbPort"]};database={_appSetting["IdentityDbName"]};user id={_appSetting["DbUser"]};password={_appSetting["DbPassword"]};Ssl Mode={_appSetting["DbSslMode"]};{trustMode}";

            optionsBuilder.UseNpgsql(connectionString, c =>
            {
                c.MigrationsAssembly(typeof(OpenIdcDbContext).Assembly.FullName);
                c.EnableRetryOnFailure(30, TimeSpan.FromSeconds(2), null);
            });
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var client = modelBuilder.Entity<RegisteredClient>();
            client.ToTable("RegisteredClients");
            client.HasKey(x => x.Id);
            client.Property(x => x.ClientId).IsRequired().HasMaxLength(64);
            client.HasIndex(x => x.ClientId).IsUnique();
            client.Property(x => x.ClientSecretHash).HasMaxLength(64);
            client.Property(x => x.DisplayName).IsRequired().HasMaxLength(100);
            client.Property(x => x.ClientType).IsRequired().HasMaxLength(8);
            client.Property(x => x.OwnerSubject).IsRequired().HasMaxLength(256);
            client.HasIndex(x => x.OwnerSubject);
            client.Property(x => x.RedirectUris).IsRequired();
        }
    }

    /// <summary>Used only by `dotnet ef` at design time; no database connection is made.</summary>
    public class OpenIdcDbContextDesignTimeFactory : IDesignTimeDbContextFactory<OpenIdcDbContext>
    {
        public OpenIdcDbContext CreateDbContext(string[] args)
        {
            var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string>
            {
                { "AppSetting:DbHost", "localhost" },
                { "AppSetting:DbPort", "5432" },
                { "AppSetting:IdentityDbName", "OpenIdc" },
                { "AppSetting:DbUser", "postgres" },
                { "AppSetting:DbPassword", "postgres" },
                { "AppSetting:DbSslMode", "Allow" },
            }).Build();
            return new OpenIdcDbContext(new AppSetting(config) { PrefferAppsettingFile = true });
        }
    }
}
