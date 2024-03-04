using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Repos.Migrations
{
    /// <inheritdoc />
    public partial class deleteuser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<long>(
                name: "DeviceId",
                table: "Users",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true,
                oldDefaultValueSql: "0");

            migrationBuilder.AddColumn<bool>(
                name: "Delete",
                table: "Invitations",
                type: "boolean",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Delete",
                table: "Invitations");

            migrationBuilder.AlterColumn<long>(
                name: "DeviceId",
                table: "Users",
                type: "bigint",
                nullable: true,
                defaultValueSql: "0",
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);
        }
    }
}