using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PraxisNote.Infrastructure.Migrations.Data.FeatureNotifications
{
    public partial class AddNotificationFridayDateShortcut : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                INSERT INTO "FeatureNotifications" ("Type", "Title", "Summary", "IssueUrl", "CreatedAt")
                VALUES (
                    'Feature',
                    'Friday due date shortcut',
                    'Quickly set task due dates to next Friday with the new "Fri" button in the date picker.',
                    'https://github.com/garethbaumgart/praxis-note/issues/219',
                    '2026-01-27T09:41:00Z'
                );
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM "FeatureNotifications"
                WHERE "Title" = 'Friday due date shortcut'
                  AND "IssueUrl" = 'https://github.com/garethbaumgart/praxis-note/issues/219'
                  AND "CreatedAt" = '2026-01-27T09:41:00Z';
                """);
        }
    }
}
