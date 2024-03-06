using ConfigurationManager;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Models;
using NodaTime;
using PostSharp.Extensibility;
using Repos.CompiledModels;
using Serilog;
using Serilog.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Repos
{
    [Profile(AttributeTargetElements = MulticastTargets.Method)]
    public class PassiDbContext : DbContext, IDataProtectionKeyContext
    {
        private AppSetting _appSetting;
        private CurrentContext _currentContext;
        public string _connectionString;
        private ILogger _logger;
        private readonly Guid _id;

        public PassiDbContext()
        {
            _logger = Logger.None;
            var myConfiguration = new Dictionary<string, string>
            {
                {"AppSetting:DbName", "Passi"},
                {"AppSetting:DbUser", "postgres"},
                {"AppSetting:DbPassword", "test1"},
                {"AppSetting:DbHost", "localhost"},
                {"AppSetting:DbPort", "5432"},
                {"AppSetting:DbSslMode", "prefer"},
            };
            var config = new ConfigurationBuilder().AddInMemoryCollection(myConfiguration).Build();
            var appSetting = new AppSetting(config);
            appSetting.PrefferAppsettingFile = true;
            _appSetting = appSetting;
        }

        public PassiDbContext(AppSetting appSetting, CurrentContext currentContext, ILogger logger)
        {
            _appSetting = appSetting;
            _currentContext = currentContext;
            _logger = logger;
            _id = Guid.NewGuid();
        }

        public DbSet<CertificateDb> Certificates { get; set; }
        public DbSet<UserDb> Users { get; set; }
        public DbSet<DeviceDb> Devices { get; set; }
        public DbSet<UserInvitationDb> Invitations { get; set; }
        public DbSet<AdminDb> Admins { get; set; }
        public DbSet<DataProtectionKey> DataProtectionKeys { get; set; }
        public DbSet<SimpleSessionDb> Sessions { get; set; }

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
            optionsBuilder.AddInterceptors(new TaggedQueryCommandInterceptor(_logger));
            if (string.IsNullOrEmpty(_connectionString))
            {
                var trustMode = _appSetting["DbSslMode"] == "Require" ? "Trust Server Certificate=true;" : "";
                _connectionString = $"host={_appSetting["DbHost"]};port={_appSetting["DbPort"]};database={_appSetting["DbName"]};user id={_appSetting["DbUser"]};password={_appSetting["DbPassword"]};Ssl Mode={_appSetting["DbSslMode"]};{trustMode}";
            }
            Console.WriteLine(_connectionString);
            optionsBuilder.UseNpgsql(_connectionString, o =>
            {
                o.UseNodaTime();
                o.EnableRetryOnFailure(30, TimeSpan.FromSeconds(2), null);
            });
            optionsBuilder.UseModel(PassiDbContextModel.Instance);
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

                entity.Property(e => e.CreationTime);

                entity.Property(e => e.ModifiedTime);

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
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Id).ValueGeneratedOnAdd().UseIdentityColumn(); ;
                entity.HasIndex(e => new { e.DeviceId, e.Platform }, "IX_Devices_DeviceId_Platform")
                    .IsUnique();

                entity.HasIndex(e => e.ModifiedById, "IX_Devices_ModifiedById");

                entity.HasIndex(e => e.NotificationToken, "IX_Devices_NotificationToken")
                    .IsUnique();

                entity.Property(e => e.CreationTime);

                entity.Property(e => e.DeviceId)
                    .IsRequired()
                    .HasMaxLength(256);

                entity.Property(e => e.ModifiedTime);

                entity.Property(e => e.NotificationToken).HasMaxLength(1024);

                entity.Property(e => e.Platform).HasMaxLength(256);

                entity.HasOne(d => d.ModifiedBy)
                    .WithMany(p => p.Devices)
                    .HasForeignKey(d => d.ModifiedById)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<UserInvitationDb>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Id).ValueGeneratedOnAdd().UseIdentityColumn(); ;
                entity.HasIndex(e => e.ModifiedById, "IX_Invitations_ModifiedById");

                entity.HasIndex(e => e.UserId, "IX_Invitations_UserId");

                entity.Property(e => e.Code).HasMaxLength(10);

                entity.Property(e => e.CreationTime);

                entity.Property(e => e.ModifiedTime);

                entity.HasOne(d => d.ModifiedBy)
                    .WithMany(p => p.InvitationModifiedBies)
                    .HasForeignKey(d => d.ModifiedById)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(d => d.User)
                    .WithMany(p => p.Invitations)
                    .HasForeignKey(d => d.UserId);
            });

            modelBuilder.Entity<SimpleSessionDb>(entity =>
            {
                entity.HasKey(e => e.Guid);

                entity.HasIndex(e => e.CreationTime, "IX_Sessions_CreationTime");

                entity.HasIndex(e => e.ModifiedById, "IX_Sessions_ModifiedById");

                entity.HasIndex(e => e.Status, "IX_Sessions_Status");

                entity.HasIndex(e => e.UserId, "IX_Sessions_UserId");
                entity.Property(e => e.SignedHashNew).HasMaxLength(1024);

                entity.Property(e => e.CreationTime);

                entity.Property(e => e.ExpirationTime)
                    ;

                entity.Property(e => e.ModifiedTime);

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
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Id).ValueGeneratedOnAdd().UseIdentityColumn(); ;

                entity.HasIndex(e => e.DeviceId, "IX_Users_DeviceId");

                entity.HasIndex(e => e.EmailHash, "IX_Users_EmailHash")
                    .IsUnique();

                entity.HasIndex(e => e.Guid, "IX_Users_Guid")
                    .IsUnique();

                entity.HasIndex(e => e.ModifiedById, "IX_Users_ModifiedById");

                entity.Property(e => e.CreationTime);

                entity.Property(e => e.EmailHash).HasMaxLength(256);

                entity.Property(e => e.ModifiedTime);

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
    }
}