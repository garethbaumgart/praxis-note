using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PraxisNote.Infrastructure.Migrations.Data.FeatureNotifications
{
    public partial class AddNotificationCollapsibleSidebar : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                INSERT INTO "FeatureNotifications" ("Type", "Title", "Summary", "IssueUrl", "CreatedAt")
                VALUES (
                    'Feature',
                    'Collapsible sidebar',
                    'Toggle the desktop sidebar between expanded and collapsed (icon-only) modes. Your preference is saved across sessions.',
                    'https://github.com/garethbaumgart/praxis-note/issues/353',
                    '2026-02-06T23:00:00Z'
                );
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM "FeatureNotifications"
                WHERE "Type" = 'Feature'
                  AND "Title" = 'Collapsible sidebar'
                  AND "IssueUrl" = 'https://github.com/garethbaumgart/praxis-note/issues/353'
                  AND "CreatedAt" = '2026-02-06T23:00:00Z';
                """);
        }
    }
}
