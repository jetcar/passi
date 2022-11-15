﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using Repos;

namespace Repos.Migrations
{
    [DbContext(typeof(PassiDbContext))]
    [Migration("20220509191802_expirationTime")]
    partial class expirationTime
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("Relational:MaxIdentifierLength", 63)
                .HasAnnotation("ProductVersion", "5.0.15")
                .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            modelBuilder.Entity("Models.CertificateDb", b =>
                {
                    b.Property<string>("Thumbprint")
                        .HasMaxLength(256)
                        .HasColumnType("character varying(256)");

                    b.Property<DateTime>("CreationTime")
                        .HasColumnType("timestamp without time zone");

                    b.Property<long?>("ModifiedById")
                        .HasColumnType("bigint");

                    b.Property<DateTime?>("ModifiedTime")
                        .HasColumnType("timestamp without time zone");

                    b.Property<string>("ParentCertId")
                        .HasColumnType("character varying(256)");

                    b.Property<string>("ParentCertSignature")
                        .HasMaxLength(1024)
                        .HasColumnType("character varying(1024)");

                    b.Property<string>("PrivateCert")
                        .HasMaxLength(1024)
                        .HasColumnType("character varying(1024)");

                    b.Property<string>("PublicCert")
                        .HasMaxLength(1024)
                        .HasColumnType("character varying(1024)");

                    b.Property<long>("UserId")
                        .HasColumnType("bigint");

                    b.HasKey("Thumbprint");

                    b.HasIndex("ModifiedById");

                    b.HasIndex("ParentCertId")
                        .IsUnique();

                    b.HasIndex("UserId");

                    b.ToTable("Certificates");
                });

            modelBuilder.Entity("Models.DeviceDb", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint")
                        .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

                    b.Property<DateTime>("CreationTime")
                        .HasColumnType("timestamp without time zone");

                    b.Property<string>("DeviceId")
                        .HasMaxLength(256)
                        .HasColumnType("character varying(256)");

                    b.Property<long?>("ModifiedById")
                        .HasColumnType("bigint");

                    b.Property<DateTime?>("ModifiedTime")
                        .HasColumnType("timestamp without time zone");

                    b.Property<string>("NotificationToken")
                        .HasMaxLength(1024)
                        .HasColumnType("character varying(1024)");

                    b.HasKey("Id");

                    b.HasIndex("DeviceId");

                    b.HasIndex("ModifiedById");

                    b.ToTable("Devices");
                });

            modelBuilder.Entity("Models.SessionDb", b =>
                {
                    b.Property<Guid>("Guid")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<string>("CheckColor")
                        .HasMaxLength(16)
                        .HasColumnType("character varying(16)");

                    b.Property<string>("ClientId")
                        .HasMaxLength(50)
                        .HasColumnType("character varying(50)");

                    b.Property<DateTime>("CreationTime")
                        .HasColumnType("timestamp without time zone");

                    b.Property<DateTime>("ExpirationTime")
                        .HasColumnType("timestamp without time zone");

                    b.Property<long?>("ModifiedById")
                        .HasColumnType("bigint");

                    b.Property<DateTime?>("ModifiedTime")
                        .HasColumnType("timestamp without time zone");

                    b.Property<string>("PublicCertThumbprint")
                        .HasMaxLength(256)
                        .HasColumnType("character varying(256)");

                    b.Property<string>("RandomString")
                        .IsRequired()
                        .HasMaxLength(256)
                        .HasColumnType("character varying(256)");

                    b.Property<string>("ReturnUrl")
                        .HasMaxLength(256)
                        .HasColumnType("character varying(256)");

                    b.Property<string>("SignedHash")
                        .HasMaxLength(1024)
                        .HasColumnType("character varying(1024)");

                    b.Property<int?>("Status")
                        .HasColumnType("integer");

                    b.Property<long>("UserId")
                        .HasColumnType("bigint");

                    b.HasKey("Guid");

                    b.HasIndex("CreationTime");

                    b.HasIndex("ModifiedById");

                    b.HasIndex("Status");

                    b.HasIndex("UserId");

                    b.ToTable("Sessions");
                });

            modelBuilder.Entity("Models.UserDb", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint")
                        .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

                    b.Property<DateTime>("CreationTime")
                        .HasColumnType("timestamp without time zone");

                    b.Property<long>("DeviceId")
                        .HasColumnType("bigint");

                    b.Property<Guid>("Guid")
                        .HasColumnType("uuid");

                    b.Property<long?>("ModifiedById")
                        .HasColumnType("bigint");

                    b.Property<DateTime?>("ModifiedTime")
                        .HasColumnType("timestamp without time zone");

                    b.Property<string>("Username")
                        .HasMaxLength(256)
                        .HasColumnType("character varying(256)");

                    b.HasKey("Id");

                    b.HasIndex("DeviceId");

                    b.HasIndex("Guid")
                        .IsUnique();

                    b.HasIndex("ModifiedById");

                    b.HasIndex("Username");

                    b.ToTable("Users");
                });

            modelBuilder.Entity("Models.UserInvitationDb", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint")
                        .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

                    b.Property<string>("Code")
                        .HasMaxLength(10)
                        .HasColumnType("character varying(10)");

                    b.Property<DateTime>("CreationTime")
                        .HasColumnType("timestamp without time zone");

                    b.Property<bool>("IsConfirmed")
                        .HasColumnType("boolean");

                    b.Property<long?>("ModifiedById")
                        .HasColumnType("bigint");

                    b.Property<DateTime?>("ModifiedTime")
                        .HasColumnType("timestamp without time zone");

                    b.Property<long>("UserId")
                        .HasColumnType("bigint");

                    b.HasKey("Id");

                    b.HasIndex("ModifiedById");

                    b.HasIndex("UserId");

                    b.ToTable("Invitations");
                });

            modelBuilder.Entity("Models.CertificateDb", b =>
                {
                    b.HasOne("Models.UserDb", "ModifiedBy")
                        .WithMany()
                        .HasForeignKey("ModifiedById");

                    b.HasOne("Models.CertificateDb", "ParentCert")
                        .WithOne("ChildCert")
                        .HasForeignKey("Models.CertificateDb", "ParentCertId");

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
                        .WithMany()
                        .HasForeignKey("ModifiedById");

                    b.Navigation("ModifiedBy");
                });

            modelBuilder.Entity("Models.SessionDb", b =>
                {
                    b.HasOne("Models.UserDb", "ModifiedBy")
                        .WithMany()
                        .HasForeignKey("ModifiedById");

                    b.HasOne("Models.UserDb", "User")
                        .WithMany()
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
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Models.UserDb", "ModifiedBy")
                        .WithMany()
                        .HasForeignKey("ModifiedById");

                    b.Navigation("Device");

                    b.Navigation("ModifiedBy");
                });

            modelBuilder.Entity("Models.UserInvitationDb", b =>
                {
                    b.HasOne("Models.UserDb", "ModifiedBy")
                        .WithMany()
                        .HasForeignKey("ModifiedById");

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
                    b.Navigation("ChildCert");
                });

            modelBuilder.Entity("Models.DeviceDb", b =>
                {
                    b.Navigation("Users");
                });

            modelBuilder.Entity("Models.UserDb", b =>
                {
                    b.Navigation("Certificates");

                    b.Navigation("Invitations");
                });
#pragma warning restore 612, 618
        }
    }
}
