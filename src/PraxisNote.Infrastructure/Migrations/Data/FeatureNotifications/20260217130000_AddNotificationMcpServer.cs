using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PraxisNote.Infrastructure.Migrations.Data.FeatureNotifications
{
    public partial class AddNotificationMcpServer : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                INSERT INTO "FeatureNotifications" ("Type", "Title", "Summary", "IssueUrl", "CreatedAt")
                VALUES (
                    'Feature',
                    'MCP server for AI assistants',
                    'Connect OpenClaw or Claude Desktop to read and write your tasks, notes, meetings, and tags via personal API keys.',
                    'https://github.com/garethbaumgart/praxis-note/pull/586',
                    '2026-02-17T13:00:00Z'
                );
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM "FeatureNotifications"
                WHERE "Title" = 'MCP server for AI assistants';
                """);
        }
    }
}
