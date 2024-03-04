using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Repos.Migrations
{
    /// <inheritdoc />
    public partial class simplifySession : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CheckColor",
                table: "Sessions");

            migrationBuilder.DropColumn(
                name: "ClientId",
                table: "Sessions");

            migrationBuilder.DropColumn(
                name: "ErrorMessage",
                table: "Sessions");

            migrationBuilder.DropColumn(
                name: "PublicCertThumbprint",
                table: "Sessions");

            migrationBuilder.DropColumn(
                name: "RandomString",
                table: "Sessions");

            migrationBuilder.DropColumn(
                name: "ReturnUrl",
                table: "Sessions");

            migrationBuilder.DropColumn(
                name: "SignedHash",
                table: "Sessions");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CheckColor",
                table: "Sessions",
                type: "character varying(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ClientId",
                table: "Sessions",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ErrorMessage",
                table: "Sessions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PublicCertThumbprint",
                table: "Sessions",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RandomString",
                table: "Sessions",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ReturnUrl",
                table: "Sessions",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SignedHash",
                table: "Sessions",
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: true);
        }
    }
}