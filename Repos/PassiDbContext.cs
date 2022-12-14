using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using ConfigurationManager;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Models;
using Npgsql;
using Serilog;
using Serilog.Core;
using NodaTime;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Repos.CompiledModels;
using static NodaTime.TimeZones.ZoneEqualityComparer;

namespace Repos
{
    public class PassiDbContext : DbContext, IDataProtectionKeyContext
    {
        private AppSetting _appSetting;
        private CurrentContext _currentContext;
        private string _connectionString;
        private ILogger _logger;

        public PassiDbContext()
        {
            AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

            _logger = Logger.None;
            var myConfiguration = new Dictionary<string, string>
            {
                {"AppSetting:DbName", "Passi"},
                {"AppSetting:DbUser", "postgres"},
                {"AppSetting:DbPassword", "q"},
                {"AppSetting:DbHost", "localhost"},
                {"AppSetting:DbSslMode", "prefer"},
            };
            var config = new ConfigurationBuilder().AddInMemoryCollection(myConfiguration).Build();
            var appSetting = new AppSetting(config);
            appSetting.PrefferAppsettingFile = true;
            _appSetting = appSetting;
        }

        public PassiDbContext(AppSetting appSetting, CurrentContext currentContext, ILogger logger)
        {
            AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

            _appSetting = appSetting;
            _currentContext = currentContext;
            _logger = logger;
        }

        public DbSet<CertificateDb> Certificates { get; set; }
        public DbSet<UserDb> Users { get; set; }
        public DbSet<DeviceDb> Devices { get; set; }
        public DbSet<UserInvitationDb> Invitations { get; set; }
        public DbSet<SessionDb> Sessions { get; set; }
        public DbSet<AdminDb> Admins { get; set; }

        public override int SaveChanges()
        {
            this.ChangeTracker.DetectChanges();
            var items = this.ChangeTracker.Entries<BaseModel>().ToList();
            foreach (var item in items.Where(x => x.State == EntityState.Added))
            {
                item.Entity.ModifiedTime = SystemClock.Instance.GetCurrentInstant();
                item.Entity.CreationTime = SystemClock.Instance.GetCurrentInstant();
                item.Entity.ModifiedById = _currentContext.CurrentUserId;
            }

            foreach (var item in items.Where(x => x.State == EntityState.Modified))
            {
                item.Entity.ModifiedTime = SystemClock.Instance.GetCurrentInstant();
                item.Entity.ModifiedById = _currentContext.CurrentUserId;
            }

            return base.SaveChanges();
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseModel(PassiDbContextModel.Instance);
            var trustMode = _appSetting["DbSslMode"] == "Require" ? "Trust Server Certificate=true;" : "";
            //optionsBuilder.AddInterceptors(new TaggedQueryCommandInterceptor(_logger));
            _connectionString = $"host={_appSetting["DbHost"]};database={_appSetting["DbName"]};user id={_appSetting["DbUser"]};password={_appSetting["DbPassword"]};Ssl Mode={_appSetting["DbSslMode"]};{trustMode}";
            Console.WriteLine(_connectionString);
            optionsBuilder.UseNpgsql(_connectionString, o => o.UseNodaTime());
            //optionsBuilder.UseModel(PassiDbContextModel.Instance);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            //while (!System.Diagnostics.Debugger.IsAttached)
            //    System.Threading.Thread.Sleep(10);

            modelBuilder.Entity<CertificateDb>(entity =>
            {
                entity.HasKey(e => e.Thumbprint);

                entity.HasIndex(e => e.ModifiedById, "IX_Certificates_ModifiedById");

                entity.HasIndex(e => e.ParentCertId, "IX_Certificates_ParentCertId")
                    .IsUnique();

                entity.HasIndex(e => e.UserId, "IX_Certificates_UserId");

                entity.Property(e => e.Thumbprint).HasMaxLength(256);

                entity.Property(e => e.CreationTime).HasColumnType("timestamp without time zone");

                entity.Property(e => e.ModifiedTime).HasColumnType("timestamp without time zone");

                entity.Property(e => e.ParentCertId).HasMaxLength(256);

                entity.Property(e => e.ParentCertSignature).HasMaxLength(1024);

                entity.Property(e => e.PrivateCert).HasMaxLength(1024);

                entity.Property(e => e.PublicCert).HasMaxLength(2048);

                entity.HasOne(d => d.ModifiedBy)
                    .WithMany(p => p.CertificateModifiedBies)
                    .HasForeignKey(d => d.ModifiedById)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(d => d.ParentCert)
                    .WithOne(p => p.InverseParentCert)
                    .HasForeignKey<CertificateDb>(d => d.ParentCertId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(d => d.User)
                    .WithMany(p => p.Certificates)
                    .HasForeignKey(d => d.UserId);
            });

            modelBuilder.Entity<DeviceDb>(entity =>
            {
                entity.HasIndex(e => new { e.DeviceId, e.Platform }, "IX_Devices_DeviceId_Platform")
                    .IsUnique();

                entity.HasIndex(e => e.ModifiedById, "IX_Devices_ModifiedById");

                entity.HasIndex(e => e.NotificationToken, "IX_Devices_NotificationToken")
                    .IsUnique();

                entity.Property(e => e.CreationTime).HasColumnType("timestamp without time zone");

                entity.Property(e => e.DeviceId)
                    .IsRequired()
                    .HasMaxLength(256);

                entity.Property(e => e.ModifiedTime).HasColumnType("timestamp without time zone");

                entity.Property(e => e.NotificationToken).HasMaxLength(1024);

                entity.Property(e => e.Platform).HasMaxLength(256);

                entity.HasOne(d => d.ModifiedBy)
                    .WithMany(p => p.Devices)
                    .HasForeignKey(d => d.ModifiedById)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<UserInvitationDb>(entity =>
            {
                entity.HasIndex(e => e.ModifiedById, "IX_Invitations_ModifiedById");

                entity.HasIndex(e => e.UserId, "IX_Invitations_UserId");

                entity.Property(e => e.Code).HasMaxLength(10);

                entity.Property(e => e.CreationTime).HasColumnType("timestamp without time zone");

                entity.Property(e => e.ModifiedTime).HasColumnType("timestamp without time zone");

                entity.HasOne(d => d.ModifiedBy)
                    .WithMany(p => p.InvitationModifiedBies)
                    .HasForeignKey(d => d.ModifiedById)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(d => d.User)
                    .WithMany(p => p.Invitations)
                    .HasForeignKey(d => d.UserId);
            });

            modelBuilder.Entity<SessionDb>(entity =>
            {
                entity.HasKey(e => e.Guid);

                entity.HasIndex(e => e.CreationTime, "IX_Sessions_CreationTime");

                entity.HasIndex(e => e.ModifiedById, "IX_Sessions_ModifiedById");

                entity.HasIndex(e => e.Status, "IX_Sessions_Status");

                entity.HasIndex(e => e.UserId, "IX_Sessions_UserId");

                entity.Property(e => e.CheckColor).HasMaxLength(16);

                entity.Property(e => e.ClientId).HasMaxLength(50);

                entity.Property(e => e.CreationTime).HasColumnType("timestamp without time zone");

                entity.Property(e => e.ExpirationTime)
                    .HasColumnType("timestamp without time zone");

                entity.Property(e => e.ModifiedTime).HasColumnType("timestamp without time zone");

                entity.Property(e => e.PublicCertThumbprint).HasMaxLength(256);

                entity.Property(e => e.RandomString)
                    .IsRequired()
                    .HasMaxLength(256);

                entity.Property(e => e.ReturnUrl).HasMaxLength(256);

                entity.Property(e => e.SignedHash).HasMaxLength(1024);

                entity.HasOne(d => d.ModifiedBy)
                    .WithMany(p => p.SessionModifiedBies)
                    .HasForeignKey(d => d.ModifiedById)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(d => d.User)
                    .WithMany(p => p.SessionUsers)
                    .HasForeignKey(d => d.UserId);
            });

            modelBuilder.Entity<UserDb>(entity =>
            {
                entity.HasIndex(e => e.DeviceId, "IX_Users_DeviceId");

                entity.HasIndex(e => e.EmailHash, "IX_Users_EmailHash")
                    .IsUnique();

                entity.HasIndex(e => e.Guid, "IX_Users_Guid")
                    .IsUnique();

                entity.HasIndex(e => e.ModifiedById, "IX_Users_ModifiedById");

                entity.Property(e => e.CreationTime).HasColumnType("timestamp without time zone");

                entity.Property(e => e.DeviceId).HasDefaultValueSql("0");

                entity.Property(e => e.EmailHash).HasMaxLength(256);

                entity.Property(e => e.ModifiedTime).HasColumnType("timestamp without time zone");

                entity.HasOne(d => d.Device)
                    .WithMany(p => p.Users)
                    .HasForeignKey(d => d.DeviceId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(d => d.ModifiedBy)
                    .WithMany(p => p.InverseModifiedBy)
                    .HasForeignKey(d => d.ModifiedById)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<AdminDb>(entity =>
            {
                entity.Property(e => e.Email).HasMaxLength(256);
                entity.HasKey(x => x.Email);
                entity.HasIndex(x => x.Email).IsUnique();
            }
            );

            base.OnModelCreating(modelBuilder);
        }

        public DbSet<DataProtectionKey> DataProtectionKeys { get; set; }
    }
}