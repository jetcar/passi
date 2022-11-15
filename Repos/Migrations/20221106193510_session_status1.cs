using Microsoft.EntityFrameworkCore.Migrations;
using NodaTime;

#nullable disable

namespace Repos.Migrations
{
    public partial class session_status2 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Instant>(
                name: "CreationTime",
                table: "Users",
                type: "timestamp without time zone",
                nullable: false,
                defaultValue: NodaTime.Instant.FromUnixTimeTicks(0L),
                oldClrType: typeof(Instant),
                oldType: "timestamp without time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<Instant>(
                name: "CreationTime",
                table: "Sessions",
                type: "timestamp without time zone",
                nullable: false,
                defaultValue: NodaTime.Instant.FromUnixTimeTicks(0L),
                oldClrType: typeof(Instant),
                oldType: "timestamp without time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<Instant>(
                name: "CreationTime",
                table: "Invitations",
                type: "timestamp without time zone",
                nullable: false,
                defaultValue: NodaTime.Instant.FromUnixTimeTicks(0L),
                oldClrType: typeof(Instant),
                oldType: "timestamp without time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<Instant>(
                name: "CreationTime",
                table: "Devices",
                type: "timestamp without time zone",
                nullable: false,
                defaultValue: NodaTime.Instant.FromUnixTimeTicks(0L),
                oldClrType: typeof(Instant),
                oldType: "timestamp without time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<Instant>(
                name: "CreationTime",
                table: "Certificates",
                type: "timestamp without time zone",
                nullable: false,
                defaultValue: NodaTime.Instant.FromUnixTimeTicks(0L),
                oldClrType: typeof(Instant),
                oldType: "timestamp without time zone",
                oldNullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Instant>(
                name: "CreationTime",
                table: "Users",
                type: "timestamp without time zone",
                nullable: true,
                oldClrType: typeof(Instant),
                oldType: "timestamp without time zone");

            migrationBuilder.AlterColumn<Instant>(
                name: "CreationTime",
                table: "Sessions",
                type: "timestamp without time zone",
                nullable: true,
                oldClrType: typeof(Instant),
                oldType: "timestamp without time zone");

            migrationBuilder.AlterColumn<Instant>(
                name: "CreationTime",
                table: "Invitations",
                type: "timestamp without time zone",
                nullable: true,
                oldClrType: typeof(Instant),
                oldType: "timestamp without time zone");

            migrationBuilder.AlterColumn<Instant>(
                name: "CreationTime",
                table: "Devices",
                type: "timestamp without time zone",
                nullable: true,
                oldClrType: typeof(Instant),
                oldType: "timestamp without time zone");

            migrationBuilder.AlterColumn<Instant>(
                name: "CreationTime",
                table: "Certificates",
                type: "timestamp without time zone",
                nullable: true,
                oldClrType: typeof(Instant),
                oldType: "timestamp without time zone");
        }
    }
}
