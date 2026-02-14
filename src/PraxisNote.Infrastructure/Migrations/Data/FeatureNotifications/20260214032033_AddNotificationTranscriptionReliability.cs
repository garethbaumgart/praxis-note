using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PraxisNote.Infrastructure.Migrations.Data.FeatureNotifications
{
    public partial class AddNotificationTranscriptionReliability : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                INSERT INTO "FeatureNotifications" ("Type", "Title", "Summary", "IssueUrl", "CreatedAt")
                VALUES (
                    'Improvement',
                    'More reliable live transcription',
                    'Recording sessions are now more stable with automatic keepalive during pauses, better reconnection handling, and fixes for audio buffer corruption.',
                    'https://github.com/garethbaumgart/praxis-note/issues/508',
                    '2026-02-14T03:20:33Z'
                );
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM "FeatureNotifications"
                WHERE "Title" = 'More reliable live transcription';
                """);
        }
    }
}
