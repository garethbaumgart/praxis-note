using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PraxisNote.Infrastructure.Migrations.Data.FeatureNotifications
{
    public partial class AddNotificationTranscriptImport : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                INSERT INTO "FeatureNotifications" ("Type", "Title", "Summary", "IssueUrl", "CreatedAt")
                VALUES (
                    'Feature',
                    'Transcript import',
                    'Import meetings from Google Gemini notes or any transcript. Paste text or upload .txt/.docx files and AI extracts titles, dates, attendees, summaries, and action items.',
                    'https://github.com/garethbaumgart/praxis-note/issues/640',
                    '2026-02-24T07:32:00Z'
                );
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM "FeatureNotifications"
                WHERE "Title" = 'Transcript import';
                """);
        }
    }
}
