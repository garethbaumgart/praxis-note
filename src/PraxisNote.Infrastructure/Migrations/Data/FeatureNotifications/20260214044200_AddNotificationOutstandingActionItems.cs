using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PraxisNote.Infrastructure.Migrations.Data.FeatureNotifications
{
    public partial class AddNotificationOutstandingActionItems : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Remove the old Daily Summary notification that references /summary (route no longer exists)
            migrationBuilder.Sql("""
                DELETE FROM "FeatureNotifications"
                WHERE "Title" = 'Daily Summary page';
                """);

            migrationBuilder.Sql("""
                INSERT INTO "FeatureNotifications" ("Type", "Title", "Summary", "IssueUrl", "CreatedAt")
                VALUES (
                    'Feature',
                    'Action items on Home page',
                    'Outstanding action items from your meetings now appear directly on the Home page — no need to visit a separate Summary page.',
                    'https://github.com/garethbaumgart/praxis-note/pull/523',
                    '2026-02-14T04:42:00Z'
                );
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM "FeatureNotifications"
                WHERE "Title" = 'Action items on Home page';
                """);

            // Restore the old Daily Summary notification
            migrationBuilder.Sql("""
                INSERT INTO "FeatureNotifications" ("Type", "Title", "Summary", "IssueUrl", "CreatedAt")
                VALUES (
                    'Feature',
                    'Daily Summary page',
                    'Review your day from /summary — see meetings, completed tasks, notes updated, and outstanding action items with date navigation.',
                    'https://github.com/garethbaumgart/praxis-note/issues/238',
                    '2026-02-07T12:00:00Z'
                );
                """);
        }
    }
}
