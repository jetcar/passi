using Microsoft.EntityFrameworkCore.Migrations;

namespace Repos.Migrations
{
    public partial class publicCertsession : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PublicCertThumbprint",
                table: "Sessions",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PublicCertThumbprint",
                table: "Sessions");
        }
    }
}