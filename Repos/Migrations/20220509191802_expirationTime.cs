using Microsoft.EntityFrameworkCore.Migrations;
using System;

namespace Repos.Migrations
{
    public partial class expirationTime : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("Truncate table \"Sessions\"");

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpirationTime",
                table: "Sessions",
                type: "timestamp without time zone",
                nullable: false,
                defaultValue: new DateTime());
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExpirationTime",
                table: "Sessions");
        }
    }
}