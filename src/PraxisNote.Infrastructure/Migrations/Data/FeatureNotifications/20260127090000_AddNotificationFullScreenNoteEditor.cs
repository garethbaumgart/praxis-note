using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PraxisNote.Infrastructure.Migrations.Data.FeatureNotifications
{
    public partial class AddNotificationFullScreenNoteEditor : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                INSERT INTO "FeatureNotifications" ("Type", "Title", "Summary", "IssueUrl", "CreatedAt")
                VALUES (
                    'Feature',
                    'Full-screen note editor',
                    'Click any note to open a dedicated editor page with auto-save, keyboard shortcuts (Ctrl+S to save, Escape to exit), and breadcrumb navigation.',
                    'https://github.com/garethbaumgart/praxis-note/pull/246',
                    '2026-01-27T09:00:00Z'
                );
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM "FeatureNotifications"
                WHERE "Title" = 'Full-screen note editor'
                  AND "IssueUrl" = 'https://github.com/garethbaumgart/praxis-note/pull/246';
                """);
        }
    }
}
