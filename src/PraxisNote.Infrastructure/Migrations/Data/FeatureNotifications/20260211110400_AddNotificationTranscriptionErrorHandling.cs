using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PraxisNote.Infrastructure.Migrations.Data.FeatureNotifications
{
    public partial class AddNotificationTranscriptionErrorHandling : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                INSERT INTO "FeatureNotifications" ("Type", "Title", "Summary", "IssueUrl", "CreatedAt")
                VALUES (
                    'BugFix',
                    'Recording transcription reliability',
                    'Fixed an issue where meeting recordings could silently fail to produce transcripts. You''ll now see clear error messages if the transcription service encounters a problem.',
                    'https://github.com/garethbaumgart/praxis-note/issues/474',
                    '2026-02-11T11:04:00Z'
                );
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM "FeatureNotifications"
                WHERE "Title" = 'Recording transcription reliability';
                """);
        }
    }
}
