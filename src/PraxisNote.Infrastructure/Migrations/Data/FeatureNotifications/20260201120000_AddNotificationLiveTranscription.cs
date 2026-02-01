using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PraxisNote.Infrastructure.Migrations.Data.FeatureNotifications
{
    public partial class AddNotificationLiveTranscription : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                INSERT INTO "FeatureNotifications" ("Type", "Title", "Summary", "IssueUrl", "CreatedAt")
                VALUES (
                    'Feature',
                    'Live transcription with Deepgram',
                    'Meeting recordings now produce real-time speech-to-text powered by Deepgram Nova-3, replacing the unreliable browser-based transcription.',
                    'https://github.com/garethbaumgart/praxis-note/pull/301',
                    '2026-02-01T12:00:00Z'
                );
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM "FeatureNotifications"
                WHERE "Title" = 'Live transcription with Deepgram';
                """);
        }
    }
}
