using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PraxisNote.Infrastructure.Migrations.Data.FeatureNotifications
{
    public partial class AddNotificationSecureCookieFix : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                INSERT INTO "FeatureNotifications" ("Type", "Title", "Summary", "IssueUrl", "CreatedAt")
                VALUES (
                    'BugFix',
                    'Login reliability fix',
                    'Fixed an issue where you could get logged out unexpectedly when using the app behind a reverse proxy.',
                    'https://github.com/garethbaumgart/praxis-note/issues/314',
                    '2026-02-04T12:00:00Z'
                );
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM "FeatureNotifications"
                WHERE "Title" = 'Login reliability fix';
                """);
        }
    }
}
