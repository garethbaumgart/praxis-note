using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PraxisNote.Infrastructure.Migrations.Data.FeatureNotifications
{
    public partial class AddNotificationDeepgramPreflightCheck : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                INSERT INTO "FeatureNotifications" ("Type", "Title", "Summary", "IssueUrl", "CreatedAt")
                VALUES (
                    'Improvement',
                    'Better transcription error messages',
                    'The transcription pre-flight check now verifies actual connectivity to the service, catching DNS failures, invalid API keys, and network issues before recording starts.',
                    'https://github.com/garethbaumgart/praxis-note/issues/489',
                    '2026-02-12T10:33:00Z'
                );
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM "FeatureNotifications"
                WHERE "Title" = 'Better transcription error messages';
                """);
        }
    }
}
