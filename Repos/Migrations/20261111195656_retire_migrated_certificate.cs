using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Repos.Migrations
{
    /// <inheritdoc />
    public partial class retire_migrated_certificate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(name: "IX_Certificates_ParentCertId", table: "Certificates");
            migrationBuilder.CreateIndex(name: "IX_Certificates_ParentCertId", table: "Certificates", column: "ParentCertId");
            migrationBuilder.AddColumn<bool>(
                name: "IsRetired",
                table: "Certificates",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Removing this column would silently re-enable copied legacy credentials.
            migrationBuilder.Sql("""
                LOCK TABLE "Certificates" IN ACCESS EXCLUSIVE MODE;
                DO $$ BEGIN
                    IF EXISTS (SELECT 1 FROM "Certificates" WHERE "IsRetired") THEN
                        RAISE EXCEPTION 'Cannot roll back certificate retirement after a key has migrated';
                    END IF;
                END $$;
                """);
            migrationBuilder.DropColumn(
                name: "IsRetired",
                table: "Certificates");
            migrationBuilder.DropIndex(name: "IX_Certificates_ParentCertId", table: "Certificates");
            migrationBuilder.CreateIndex(name: "IX_Certificates_ParentCertId", table: "Certificates", column: "ParentCertId", unique: true);
        }
    }
}
