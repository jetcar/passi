using Microsoft.EntityFrameworkCore.Migrations;
using NodaTime;

#nullable disable

namespace Repos.Migrations
{
    public partial class session_fix : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Instant>(
                name: "ExpirationTime",
                table: "Sessions",
                type: "timestamp without time zone",
                nullable: false,
                oldClrType: typeof(Instant),
                oldType: "timestamp without time zone",
                oldDefaultValueSql: "'0001-01-01 00:00:00'::timestamp without time zone");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Instant>(
                name: "ExpirationTime",
                table: "Sessions",
                type: "timestamp without time zone",
                nullable: false,
                defaultValueSql: "'0001-01-01 00:00:00'::timestamp without time zone",
                oldClrType: typeof(Instant),
                oldType: "timestamp without time zone");
        }
    }
}
