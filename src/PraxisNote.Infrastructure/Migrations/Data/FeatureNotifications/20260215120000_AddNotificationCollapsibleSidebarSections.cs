using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PraxisNote.Infrastructure.Migrations.Data.FeatureNotifications
{
    public partial class AddNotificationCollapsibleSidebarSections : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                INSERT INTO "FeatureNotifications" ("Type", "Title", "Summary", "IssueUrl", "CreatedAt")
                VALUES (
                    'Improvement',
                    'Collapsible sidebar sections',
                    'Collapse In Progress, Up Next, and Context sections in the sidebar to reduce clutter. A count badge shows hidden items. State persists across reloads.',
                    'https://github.com/garethbaumgart/praxis-note/issues/543',
                    '2026-02-15T12:00:00Z'
                );
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM "FeatureNotifications"
                WHERE "Title" = 'Collapsible sidebar sections';
                """);
        }
    }
}
