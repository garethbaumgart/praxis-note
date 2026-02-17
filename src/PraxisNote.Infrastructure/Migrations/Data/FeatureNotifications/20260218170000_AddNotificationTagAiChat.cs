using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PraxisNote.Infrastructure.Migrations.Data.FeatureNotifications
{
    public partial class AddNotificationTagAiChat : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                INSERT INTO "FeatureNotifications" ("Type", "Title", "Summary", "IssueUrl", "CreatedAt")
                VALUES (
                    'Feature',
                    'Tag Hub AI Chat',
                    'Ask questions about your tagged notes, meetings, and tasks with conversational AI. Get answers grounded in your real data with streaming responses.',
                    'https://github.com/garethbaumgart/praxis-note/issues/339',
                    '2026-02-18T17:00:00Z'
                );
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM "FeatureNotifications"
                WHERE "Title" = 'Tag Hub AI Chat';
                """);
        }
    }
}
