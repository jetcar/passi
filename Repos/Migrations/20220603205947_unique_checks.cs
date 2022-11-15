using Microsoft.EntityFrameworkCore.Migrations;

namespace Repos.Migrations
{
    public partial class unique_checks : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            //migrationBuilder.Sql("Truncate table \"Users\"");


            migrationBuilder.DropIndex(
                name: "IX_Users_Username",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Devices_DeviceId",
                table: "Devices");

            migrationBuilder.AddColumn<string>(
                name: "Platform",
                table: "Devices",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Username",
                table: "Users",
                column: "Username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Devices_DeviceId_Platform",
                table: "Devices",
                columns: new[] { "DeviceId", "Platform" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Devices_NotificationToken",
                table: "Devices",
                column: "NotificationToken",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_Username",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Devices_DeviceId_Platform",
                table: "Devices");

            migrationBuilder.DropIndex(
                name: "IX_Devices_NotificationToken",
                table: "Devices");

            migrationBuilder.DropColumn(
                name: "Platform",
                table: "Devices");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Username",
                table: "Users",
                column: "Username");

            migrationBuilder.CreateIndex(
                name: "IX_Devices_DeviceId",
                table: "Devices",
                column: "DeviceId");
        }
    }
}
