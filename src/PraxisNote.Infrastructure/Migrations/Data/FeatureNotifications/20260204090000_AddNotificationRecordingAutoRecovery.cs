using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PraxisNote.Infrastructure.Migrations.Data.FeatureNotifications
{
    public partial class AddNotificationRecordingAutoRecovery : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                INSERT INTO "FeatureNotifications" ("Type", "Title", "Summary", "IssueUrl", "CreatedAt")
                VALUES (
                    'BugFix',
                    'Reliable meeting recordings',
                    'Meeting recordings no longer stop unexpectedly. The recorder now automatically recovers from errors and continues capturing audio.',
                    'https://github.com/garethbaumgart/praxis-note/pull/',
                    '2026-02-04T09:00:00Z'
                );
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM "FeatureNotifications"
                WHERE "Title" = 'Reliable meeting recordings';
                """);
        }
    }
}
