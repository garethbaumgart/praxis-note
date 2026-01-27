using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PraxisNote.Infrastructure.Migrations.Data.FeatureNotifications
{
    public partial class AddNotificationAiMeetingAnalysis : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                INSERT INTO "FeatureNotifications" ("Type", "Title", "Summary", "IssueUrl", "CreatedAt")
                VALUES (
                    'Feature',
                    'AI-powered meeting analysis',
                    'Analyze meeting transcripts with Claude AI to generate summaries, key discussion points, and decisions.',
                    'https://github.com/garethbaumgart/praxis-note/issues/230',
                    '2026-01-27T20:21:00Z'
                );
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM "FeatureNotifications"
                WHERE "Title" = 'AI-powered meeting analysis';
                """);
        }
    }
}
