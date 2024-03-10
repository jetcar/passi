using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ConfigurationManager;
using EFCoreSecondLevelCacheInterceptor;
using IdentityServer4.EntityFramework.Storage.Entities;
using IdentityServer4.EntityFramework.Storage.Interfaces;
using MessagePack;
using MessagePack.Formatters;
using MessagePack.Resolvers;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ILogger = Serilog.ILogger;

namespace IdentityRepo.DbContext
{
    [GoogleTracer.Profile]
    public class IdentityDbContext : Microsoft.EntityFrameworkCore.DbContext, IConfigurationDbContext,
            IDataProtectionKeyContext, IPersistedGrantDbContext
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

        public IdentityDbContext(ILogger logger) : base(new DbContextOptions<IdentityDbContext>())
        {
            AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
            _logger = logger;
            var myConfiguration = new Dictionary<string, string>
            {
                {"AppSetting:IdentityDbName", "Identity"},
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

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            //optionsBuilder.UseModel(IdentityDbContextModel.Instance);
            _logger.Debug("dbhost=" + _appSetting["DbHost"]);
            var trustMode = _appSetting["DbSslMode"] == "Require" ? "Trust Server Certificate=true;" : "";
            //optionsBuilder.AddInterceptors(new TaggedQueryCommandInterceptor(_logger));
            var connectionString = $"host={_appSetting["DbHost"]};port={_appSetting["DbPort"]};database={_appSetting["IdentityDbName"]};user id={_appSetting["DbUser"]};password={_appSetting["DbPassword"]};Ssl Mode={_appSetting["DbSslMode"]};{trustMode}";
            Console.WriteLine(connectionString);

            var dbServices = new ServiceCollection()
                .AddEntityFrameworkNpgsql();

            const string providerName1 = "Redis1";
            dbServices.AddEFSecondLevelCache(options =>
                options.UseEasyCachingCoreProvider(providerName1, isHybridCache: false).DisableLogging(false).UseCacheKeyPrefix("EF_")
                    // Fallback on db if the caching provider fails (for example, if Redis is down).
                    .UseDbCallsIfCachingProviderIsDown(TimeSpan.FromMinutes(1))
            );

            // More info: https://easycaching.readthedocs.io/en/latest/Redis/
            dbServices.AddEasyCaching(option =>
            {
                option.UseRedis(config =>
                    {
                        config.DBConfig.AllowAdmin = true;
                        config.DBConfig.SyncTimeout = 5000;
                        config.DBConfig.AsyncTimeout = 5000;
                        config.DBConfig.Endpoints.Add(new EasyCaching.Core.Configurations.ServerEndPoint(_appSetting["redis"], Convert.ToInt32(_appSetting["redisPort"])));
                        config.EnableLogging = true;
                        config.DBConfig.KeyPrefix = "passi";
                        config.SerializerName = "Pack";
                        config.DBConfig.ConnectionTimeout = 5000;
                    }, providerName1)
                    .WithMessagePack(so =>
                        {
                            so.EnableCustomResolver = true;
                            so.CustomResolvers = CompositeResolver.Create(
                                new IMessagePackFormatter[]
                                {
                                    DBNullFormatter.Instance, // This is necessary for the null values
                                },
                                new IFormatterResolver[]
                                {
                                    NativeDateTimeResolver.Instance,
                                    ContractlessStandardResolver.Instance,
                                    StandardResolverAllowPrivate.Instance,
                                });
                        },
                        "Pack");
            });

            var serviceProvider = dbServices
                .BuildServiceProvider();

            optionsBuilder.UseNpgsql(connectionString, o =>
            {
                o.EnableRetryOnFailure(30, TimeSpan.FromSeconds(2), null);
            }).UseInternalServiceProvider(serviceProvider);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<PersistedGrant>(grant =>
            {
                grant.Property(x => x.Key).HasMaxLength(200).ValueGeneratedNever();
                grant.Property(x => x.Type).HasMaxLength(50).IsRequired();
                grant.Property(x => x.SubjectId).HasMaxLength(200);
                grant.Property(x => x.SessionId).HasMaxLength(100);
                grant.Property(x => x.ClientId).HasMaxLength(200).IsRequired();
                grant.Property(x => x.Description).HasMaxLength(200);
                grant.Property(x => x.CreationTime).IsRequired();
                // 50000 chosen to be explicit to allow enough size to avoid truncation, yet stay beneath the MySql row size limit of ~65K
                // apparently anything over 4K converts to nvarchar(max) on SqlServer
                grant.Property(x => x.Data).HasMaxLength(50000).IsRequired();

                grant.HasKey(x => x.Key);

                grant.HasIndex(x => new { x.SubjectId, x.ClientId, x.Type });
                grant.HasIndex(x => new { x.SubjectId, x.SessionId, x.Type });
                grant.HasIndex(x => x.Expiration);
            });

            modelBuilder.Entity<DeviceFlowCodes>(codes =>
            {
                codes.Property(x => x.DeviceCode).HasMaxLength(200).IsRequired();
                codes.Property(x => x.UserCode).HasMaxLength(200).IsRequired();
                codes.Property(x => x.SubjectId).HasMaxLength(200);
                codes.Property(x => x.SessionId).HasMaxLength(100);
                codes.Property(x => x.ClientId).HasMaxLength(200).IsRequired();
                codes.Property(x => x.Description).HasMaxLength(200);
                codes.Property(x => x.CreationTime).IsRequired();
                codes.Property(x => x.Expiration).IsRequired();
                // 50000 chosen to be explicit to allow enough size to avoid truncation, yet stay beneath the MySql row size limit of ~65K
                // apparently anything over 4K converts to nvarchar(max) on SqlServer
                codes.Property(x => x.Data).HasMaxLength(50000).IsRequired();

                codes.HasKey(x => new { x.UserCode });

                codes.HasIndex(x => x.DeviceCode).IsUnique();
                codes.HasIndex(x => x.Expiration);
            });

            modelBuilder.Entity<UserClient>(entity =>
            {
                entity.HasKey(c => new { c.UserId, c.ClientId });
            });

            base.OnModelCreating(modelBuilder);
        }

        public DbSet<DataProtectionKey> DataProtectionKeys { get; set; }
        public DbSet<PersistedGrant> PersistedGrants { get; set; }
        public DbSet<DeviceFlowCodes> DeviceFlowCodes { get; set; }
        public DbSet<Client> Clients { get; set; }
        public DbSet<UserClient> UserClients { get; set; }
        public DbSet<ClientCorsOrigin> ClientCorsOrigins { get; set; }
        public DbSet<IdentityResource> IdentityResources { get; set; }
        public DbSet<ApiResource> ApiResources { get; set; }
        public DbSet<ApiScope> ApiScopes { get; set; }

        public Task<int> SaveChangesAsync()
        {
            return base.SaveChangesAsync();
        }
    }

    public class UserClient
    {
        public string UserId { get; set; }
        public Client Client { get; set; }
        public int ClientId { get; set; }
    }
}