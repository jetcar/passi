using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Repos.Migrations
{
    /// <inheritdoc />
    public partial class smart_home_cloud : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SmartHomeOwners",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    OwnerId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SmartHomeOwners", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SmartHomes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    EnrollmentHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    EnrollmentExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    TokenHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    TokenExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Generation = table.Column<long>(type: "bigint", nullable: false),
                    ConnectionId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    ConnectedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastSeenAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    SnapshotAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    AwaitingSnapshot = table.Column<bool>(type: "boolean", nullable: false),
                    DevicesJson = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SmartHomes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SmartHomeCommands",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    HomeId = table.Column<Guid>(type: "uuid", nullable: false),
                    DeviceId = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    On = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    SentAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    ConnectionId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ErrorCode = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SmartHomeCommands", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SmartHomeCommands_SmartHomes_HomeId",
                        column: x => x.HomeId,
                        principalTable: "SmartHomes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SmartHomeCommands_HomeId_CreatedAt",
                table: "SmartHomeCommands",
                columns: new[] { "HomeId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SmartHomes_OwnerId",
                table: "SmartHomes",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_SmartHomes_TokenHash",
                table: "SmartHomes",
                column: "TokenHash",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SmartHomeCommands");

            migrationBuilder.DropTable(
                name: "SmartHomeOwners");

            migrationBuilder.DropTable(
                name: "SmartHomes");
        }
    }
}
