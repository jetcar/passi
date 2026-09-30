using ConfigurationManager;
using Microsoft.Extensions.Configuration;
using NLog;
using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WebApp.News;

namespace WebApp
{
    [GoogleTracer.Profile]
    public class WebAppDbContext : DbContext, IDataProtectionKeyContext
    {
        private static readonly ILogger _logger = LogManager.GetCurrentClassLogger();
        private AppSetting _appSetting;
        private string _connectionString;

        //public WebAppDbContext()
        //{
        //    //_logger = logger;
        //    var myConfiguration = new Dictionary<string, string>
        //    {
        //        {"AppSetting:DbName", "WebApp"},
        //        {"AppSetting:DbUser", "postgres"},
        //        {"AppSetting:DbPassword", "q"},
        //        {"AppSetting:DbHost", "database"},
        //        {"AppSetting:DbPort", "5432"},
        //    };
        //    var config = new ConfigurationBuilder().AddInMemoryCollection(myConfiguration).Build();
        //    var appSetting = new AppSetting(config);
        //    appSetting.PrefferAppsettingFile = true;
        //    _appSetting = appSetting;
        //}

        public WebAppDbContext(AppSetting appSetting)
        {
            _appSetting = appSetting;
        }

        /// <summary>Used by tests to supply a different provider; OnConfiguring then leaves it untouched.</summary>
        public WebAppDbContext(DbContextOptions<WebAppDbContext> options) : base(options)
        {
        }

        public DbSet<DataProtectionKey> DataProtectionKeys { get; set; }
        public DbSet<NewsPost> NewsPosts { get; set; }
        public DbSet<NewsReaction> NewsReactions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<NewsPost>(post =>
            {
                post.HasKey(p => p.Id);
                post.HasIndex(p => p.Slug).IsUnique();
                post.HasIndex(p => p.PublishedAt);
                post.Property(p => p.Slug).IsRequired().HasMaxLength(NewsService.MaxSlugLength);
                post.Property(p => p.Title).IsRequired().HasMaxLength(NewsService.MaxTitleLength);
                post.Property(p => p.Summary).IsRequired().HasMaxLength(NewsService.MaxSummaryLength);
                post.Property(p => p.BodyMarkdown).IsRequired();
                post.Property(p => p.AuthorEmail).HasMaxLength(320);
            });

            modelBuilder.Entity<NewsReaction>(reaction =>
            {
                reaction.HasKey(r => new { r.PostId, r.VisitorId, r.Reaction });
                reaction.Property(r => r.VisitorId).HasMaxLength(64);
                reaction.Property(r => r.Reaction).HasMaxLength(32);
                reaction.HasOne<NewsPost>().WithMany().HasForeignKey(r => r.PostId).OnDelete(DeleteBehavior.Cascade);
            });
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (optionsBuilder.IsConfigured)
                return;

            var trustMode = _appSetting["DbSslMode"] == "Require" ? "Trust Server Certificate=true;" : "";
            //optionsBuilder.AddInterceptors(new TaggedQueryCommandInterceptor(_logger));
            _connectionString = $"host={_appSetting["DbHost"]};port={_appSetting["DbPort"]};database={_appSetting["WebAppDbName"]};user id={_appSetting["DbUser"]};password={_appSetting["DbPassword"]};Ssl Mode={_appSetting["DbSslMode"]};{trustMode}";

            optionsBuilder.UseNpgsql(_connectionString, c =>
            {
                c.MigrationsAssembly(typeof(WebAppDbContext).Assembly.FullName);
                c.EnableRetryOnFailure(30, TimeSpan.FromSeconds(2), null);
            });
        }
    }
}