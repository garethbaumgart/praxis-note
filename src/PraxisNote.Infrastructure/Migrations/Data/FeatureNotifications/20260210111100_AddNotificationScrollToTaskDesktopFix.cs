using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PraxisNote.Infrastructure.Migrations.Data.FeatureNotifications
{
    public partial class AddNotificationScrollToTaskDesktopFix : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                INSERT INTO "FeatureNotifications" ("Type", "Title", "Summary", "IssueUrl", "CreatedAt")
                VALUES (
                    'BugFix',
                    'Scroll to task fixed on desktop',
                    'Clicking a task in the sidebar now correctly scrolls to it on desktop. The highlight glow also lasts longer for easier spotting.',
                    'https://github.com/garethbaumgart/praxis-note/issues/464',
                    '2026-02-10T11:11:00Z'
                );
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM "FeatureNotifications"
                WHERE "Title" = 'Scroll to task fixed on desktop';
                """);
        }
    }
}
