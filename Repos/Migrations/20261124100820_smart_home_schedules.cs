using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Repos.Migrations
{
    /// <inheritdoc />
    public partial class smart_home_schedules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SchedulesJson",
                table: "SmartHomes",
                type: "jsonb",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.AddColumn<string>(
                name: "Operation",
                table: "SmartHomeCommands",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "power.set");

            migrationBuilder.AddColumn<string>(
                name: "PayloadJson",
                table: "SmartHomeCommands",
                type: "jsonb",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SchedulesJson",
                table: "SmartHomes");

            migrationBuilder.DropColumn(
                name: "Operation",
                table: "SmartHomeCommands");

            migrationBuilder.DropColumn(
                name: "PayloadJson",
                table: "SmartHomeCommands");
        }
    }
}
