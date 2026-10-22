using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebApp.Migrations
{
    /// <inheritdoc />
    public partial class PublishStreamdekNews : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "NewsPosts",
                columns: new[]
                {
                    "Id", "Slug", "Title", "Summary", "BodyMarkdown",
                    "PublishedAt", "CreatedAt", "UpdatedAt", "AuthorEmail"
                },
                values: new object[]
                {
                    new System.Guid("e7e5df73-0693-4bc2-8ac7-8f8655f72edc"),
                    "streamdek-agent-status-on-stream-deck",
                    "Streamdek brings Codex and Claude Code status to Stream Deck",
                    "Streamdek turns an Elgato Stream Deck into a live dashboard for Codex and Claude Code sessions, with native installers for Windows, macOS, and Linux.",
                    """
                    Streamdek is a new open-source Passi project that turns an Elgato Stream Deck into a live dashboard for AI coding work.

                    Each button shows a Codex or Claude Code session, its current state—working, waiting for input, failed, scheduled, or complete—and a short excerpt from the latest response. Pressing a button reopens the related session. Active cards keep their position, while sessions inactive for more than 24 hours disappear automatically.

                    It also groups scheduled Claude Code routines and Codex automations, and supports Windows, macOS, and Linux without requiring the Elgato desktop application.

                    Native installers bundle everything needed:

                    - Windows `.exe`
                    - macOS `.pkg` for Apple silicon and Intel
                    - Linux `.deb` for x64 and ARM64

                    [View Streamdek on GitHub](https://github.com/jetcar/streamdek) or [download the latest release](https://github.com/jetcar/streamdek/releases/latest).
                    """,
                    new System.DateTime(2026, 10, 22, 16, 5, 0, System.DateTimeKind.Utc),
                    new System.DateTime(2026, 10, 22, 16, 5, 0, System.DateTimeKind.Utc),
                    new System.DateTime(2026, 10, 22, 16, 5, 0, System.DateTimeKind.Utc),
                    null
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "NewsPosts",
                keyColumn: "Id",
                keyValue: new System.Guid("e7e5df73-0693-4bc2-8ac7-8f8655f72edc"));
        }
    }
}
