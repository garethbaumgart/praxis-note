using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PraxisNote.Infrastructure.Migrations.Data.FeatureNotifications
{
    public partial class AddNotificationScreenshotTimezone : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                INSERT INTO "FeatureNotifications" ("Type", "Title", "Summary", "IssueUrl", "CreatedAt")
                VALUES (
                    'BugFix',
                    'Screenshot import timezone fix',
                    'Calendar screenshot imports now correctly detect your timezone, so extracted meeting times match what is shown in your calendar.',
                    'https://github.com/garethbaumgart/praxis-note/pull/326',
                    '2026-02-05T04:01:00Z'
                );
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM "FeatureNotifications"
                WHERE "Title" = 'Screenshot import timezone fix';
                """);
        }
    }
}
