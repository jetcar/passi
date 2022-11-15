using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Repos.Migrations
{
    public partial class certificates : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PublicCert",
                table: "Users");

            migrationBuilder.CreateTable(
                name: "Certificates",
                columns: table => new
                {
                    Thumbprint = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    PublicCert = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    PrivateCert = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    ParentCertSignature = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    ParentCertId = table.Column<string>(type: "character varying(256)", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    ModifiedTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    ModifiedById = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Certificates", x => x.Thumbprint);
                    table.ForeignKey(
                        name: "FK_Certificates_Certificates_ParentCertId",
                        column: x => x.ParentCertId,
                        principalTable: "Certificates",
                        principalColumn: "Thumbprint",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Certificates_Users_ModifiedById",
                        column: x => x.ModifiedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Certificates_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Certificates_ModifiedById",
                table: "Certificates",
                column: "ModifiedById");

            migrationBuilder.CreateIndex(
                name: "IX_Certificates_ParentCertId",
                table: "Certificates",
                column: "ParentCertId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Certificates_UserId",
                table: "Certificates",
                column: "UserId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Certificates");

            migrationBuilder.AddColumn<string>(
                name: "PublicCert",
                table: "Users",
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: true);
        }
    }
}
