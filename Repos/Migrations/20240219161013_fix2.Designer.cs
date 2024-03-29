﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using NodaTime;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using Repos;

#nullable disable

namespace Repos.Migrations
{
    [DbContext(typeof(PassiDbContext))]
    [Migration("20240219161013_fix2")]
    partial class fix2
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.1")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("Microsoft.AspNetCore.DataProtection.EntityFrameworkCore.DataProtectionKey", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<string>("FriendlyName")
                        .HasColumnType("text");

                    b.Property<string>("Xml")
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.ToTable("DataProtectionKeys");
                });

            modelBuilder.Entity("Models.AdminDb", b =>
                {
                    b.Property<string>("Email")
                        .HasMaxLength(256)
                        .HasColumnType("character varying(256)");

                    b.HasKey("Email");

                    b.HasIndex("Email")
                        .IsUnique();

                    b.ToTable("Admins");
                });

            modelBuilder.Entity("Models.CertificateDb", b =>
                {
                    b.Property<string>("Thumbprint")
                        .HasMaxLength(256)
                        .HasColumnType("character varying(256)");

                    b.Property<Instant>("CreationTime")
                        .HasColumnType("timestamp without time zone");

                    b.Property<long?>("ModifiedById")
                        .HasColumnType("bigint");

                    b.Property<Instant?>("ModifiedTime")
                        .HasColumnType("timestamp without time zone");

                    b.Property<string>("ParentCertId")
                        .HasMaxLength(256)
                        .HasColumnType("character varying(256)");

                    b.Property<string>("ParentCertSignature")
                        .HasMaxLength(1024)
                        .HasColumnType("character varying(1024)");

                    b.Property<string>("PrivateCert")
                        .HasMaxLength(1024)
                        .HasColumnType("character varying(1024)");

                    b.Property<string>("PublicCert")
                        .HasMaxLength(2048)
                        .HasColumnType("character varying(2048)");

                    b.Property<long>("UserId")
                        .HasColumnType("bigint");

                    b.HasKey("Thumbprint");

                    b.HasIndex(new[] { "ModifiedById" }, "IX_Certificates_ModifiedById");

                    b.HasIndex(new[] { "ParentCertId" }, "IX_Certificates_ParentCertId")
                        .IsUnique();

                    b.HasIndex(new[] { "UserId" }, "IX_Certificates_UserId");

                    b.ToTable("Certificates");
                });

            modelBuilder.Entity("Models.DeviceDb", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<long>("Id"));

                    b.Property<Instant>("CreationTime")
                        .HasColumnType("timestamp without time zone");

                    b.Property<string>("DeviceId")
                        .IsRequired()
                        .HasMaxLength(256)
                        .HasColumnType("character varying(256)");

                    b.Property<long?>("ModifiedById")
                        .HasColumnType("bigint");

                    b.Property<Instant?>("ModifiedTime")
                        .HasColumnType("timestamp without time zone");

                    b.Property<string>("NotificationToken")
                        .HasMaxLength(1024)
                        .HasColumnType("character varying(1024)");

                    b.Property<string>("Platform")
                        .HasMaxLength(256)
                        .HasColumnType("character varying(256)");

                    b.HasKey("Id");

                    b.HasIndex(new[] { "DeviceId", "Platform" }, "IX_Devices_DeviceId_Platform")
                        .IsUnique();

                    b.HasIndex(new[] { "ModifiedById" }, "IX_Devices_ModifiedById");

                    b.HasIndex(new[] { "NotificationToken" }, "IX_Devices_NotificationToken")
                        .IsUnique();

                    b.ToTable("Devices");
                });

            modelBuilder.Entity("Models.SimpleSessionDb", b =>
                {
                    b.Property<Guid>("Guid")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<Instant>("CreationTime")
                        .HasColumnType("timestamp without time zone");

                    b.Property<Instant>("ExpirationTime")
                        .HasColumnType("timestamp without time zone");

                    b.Property<long?>("ModifiedById")
                        .HasColumnType("bigint");

                    b.Property<Instant?>("ModifiedTime")
                        .HasColumnType("timestamp without time zone");

                    b.Property<string>("SignedHashNew")
                        .HasMaxLength(1024)
                        .HasColumnType("character varying(1024)");

                    b.Property<int?>("Status")
                        .HasColumnType("integer");

                    b.Property<long>("UserId")
                        .HasColumnType("bigint");

                    b.HasKey("Guid");

                    b.HasIndex(new[] { "CreationTime" }, "IX_Sessions_CreationTime");

                    b.HasIndex(new[] { "ModifiedById" }, "IX_Sessions_ModifiedById");

                    b.HasIndex(new[] { "Status" }, "IX_Sessions_Status");

                    b.HasIndex(new[] { "UserId" }, "IX_Sessions_UserId");

                    b.ToTable("Sessions");
                });

            modelBuilder.Entity("Models.UserDb", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<long>("Id"));

                    b.Property<Instant>("CreationTime")
                        .HasColumnType("timestamp without time zone");

                    b.Property<long?>("DeviceId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint")
                        .HasDefaultValueSql("0");

                    b.Property<string>("EmailHash")
                        .HasMaxLength(256)
                        .HasColumnType("character varying(256)");

                    b.Property<Guid>("Guid")
                        .HasColumnType("uuid");

                    b.Property<long?>("ModifiedById")
                        .HasColumnType("bigint");

                    b.Property<Instant?>("ModifiedTime")
                        .HasColumnType("timestamp without time zone");

                    b.HasKey("Id");

                    b.HasIndex(new[] { "DeviceId" }, "IX_Users_DeviceId");

                    b.HasIndex(new[] { "EmailHash" }, "IX_Users_EmailHash")
                        .IsUnique();

                    b.HasIndex(new[] { "Guid" }, "IX_Users_Guid")
                        .IsUnique();

                    b.HasIndex(new[] { "ModifiedById" }, "IX_Users_ModifiedById");

                    b.ToTable("Users");
                });

            modelBuilder.Entity("Models.UserInvitationDb", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<long>("Id"));

                    b.Property<string>("Code")
                        .HasMaxLength(10)
                        .HasColumnType("character varying(10)");

                    b.Property<Instant>("CreationTime")
                        .HasColumnType("timestamp without time zone");

                    b.Property<bool>("IsConfirmed")
                        .HasColumnType("boolean");

                    b.Property<long?>("ModifiedById")
                        .HasColumnType("bigint");

                    b.Property<Instant?>("ModifiedTime")
                        .HasColumnType("timestamp without time zone");

                    b.Property<int?>("TryCount")
                        .HasColumnType("integer");

                    b.Property<long>("UserId")
                        .HasColumnType("bigint");

                    b.HasKey("Id");

                    b.HasIndex(new[] { "ModifiedById" }, "IX_Invitations_ModifiedById");

                    b.HasIndex(new[] { "UserId" }, "IX_Invitations_UserId");

                    b.ToTable("Invitations");
                });

            modelBuilder.Entity("Models.CertificateDb", b =>
                {
                    b.HasOne("Models.UserDb", "ModifiedBy")
                        .WithMany("CertificateModifiedBies")
                        .HasForeignKey("ModifiedById")
                        .OnDelete(DeleteBehavior.Restrict);

                    b.HasOne("Models.CertificateDb", "ParentCert")
                        .WithOne("InverseParentCert")
                        .HasForeignKey("Models.CertificateDb", "ParentCertId")
                        .OnDelete(DeleteBehavior.Restrict);

                    b.HasOne("Models.UserDb", "User")
                        .WithMany("Certificates")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("ModifiedBy");

                    b.Navigation("ParentCert");

                    b.Navigation("User");
                });

            modelBuilder.Entity("Models.DeviceDb", b =>
                {
                    b.HasOne("Models.UserDb", "ModifiedBy")
                        .WithMany("Devices")
                        .HasForeignKey("ModifiedById")
                        .OnDelete(DeleteBehavior.Restrict);

                    b.Navigation("ModifiedBy");
                });

            modelBuilder.Entity("Models.SimpleSessionDb", b =>
                {
                    b.HasOne("Models.UserDb", "ModifiedBy")
                        .WithMany("SessionModifiedBies")
                        .HasForeignKey("ModifiedById")
                        .OnDelete(DeleteBehavior.Restrict);

                    b.HasOne("Models.UserDb", "User")
                        .WithMany("SessionUsers")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("ModifiedBy");

                    b.Navigation("User");
                });

            modelBuilder.Entity("Models.UserDb", b =>
                {
                    b.HasOne("Models.DeviceDb", "Device")
                        .WithMany("Users")
                        .HasForeignKey("DeviceId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("Models.UserDb", "ModifiedBy")
                        .WithMany("InverseModifiedBy")
                        .HasForeignKey("ModifiedById")
                        .OnDelete(DeleteBehavior.Restrict);

                    b.Navigation("Device");

                    b.Navigation("ModifiedBy");
                });

            modelBuilder.Entity("Models.UserInvitationDb", b =>
                {
                    b.HasOne("Models.UserDb", "ModifiedBy")
                        .WithMany("InvitationModifiedBies")
                        .HasForeignKey("ModifiedById")
                        .OnDelete(DeleteBehavior.Restrict);

                    b.HasOne("Models.UserDb", "User")
                        .WithMany("Invitations")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("ModifiedBy");

                    b.Navigation("User");
                });

            modelBuilder.Entity("Models.CertificateDb", b =>
                {
                    b.Navigation("InverseParentCert");
                });

            modelBuilder.Entity("Models.DeviceDb", b =>
                {
                    b.Navigation("Users");
                });

            modelBuilder.Entity("Models.UserDb", b =>
                {
                    b.Navigation("CertificateModifiedBies");

                    b.Navigation("Certificates");

                    b.Navigation("Devices");

                    b.Navigation("InverseModifiedBy");

                    b.Navigation("InvitationModifiedBies");

                    b.Navigation("Invitations");

                    b.Navigation("SessionModifiedBies");

                    b.Navigation("SessionUsers");
                });
#pragma warning restore 612, 618
        }
    }
}
