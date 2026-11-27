using Microsoft.EntityFrameworkCore;
using Models;

namespace Repos;

public static class SmartHomeModel
{
    public static void Configure(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SmartHomeOwnerDb>(b =>
        {
            b.ToTable("SmartHomeOwners");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).ValueGeneratedNever();
        });
        modelBuilder.Entity<SmartHomeDb>(b =>
        {
            b.ToTable("SmartHomes");
            b.HasKey(x => x.Id);
            b.Property(x => x.Name).IsRequired().HasMaxLength(80);
            b.Property(x => x.EnrollmentHash).HasMaxLength(64);
            b.Property(x => x.TokenHash).HasMaxLength(64);
            b.Property(x => x.ConnectionId).HasMaxLength(128);
            b.Property(x => x.DevicesJson).IsRequired().HasColumnType("jsonb");
            b.Property(x => x.SchedulesJson).IsRequired().HasColumnType("jsonb").HasDefaultValue("[]");
            b.HasIndex(x => x.TokenHash).IsUnique();
            b.HasIndex(x => x.OwnerId);
        });
        modelBuilder.Entity<SmartHomeCommandDb>(b =>
        {
            b.ToTable("SmartHomeCommands");
            b.HasKey(x => x.Id);
            b.Property(x => x.DeviceId).IsRequired().HasMaxLength(80);
            b.Property(x => x.Status).IsRequired().HasMaxLength(16);
            b.Property(x => x.Operation).IsRequired().HasMaxLength(32).HasDefaultValue("power.set");
            b.Property(x => x.PayloadJson).HasColumnType("jsonb");
            b.Property(x => x.ConnectionId).HasMaxLength(128);
            b.Property(x => x.ErrorCode).HasMaxLength(80);
            b.HasIndex(x => new { x.HomeId, x.CreatedAt });
            b.HasOne<SmartHomeDb>().WithMany().HasForeignKey(x => x.HomeId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}
