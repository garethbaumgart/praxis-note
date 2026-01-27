using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PraxisNote.Infrastructure.Migrations.Data.FeatureNotifications
{
    public partial class AddNotificationSidebarHighlighting : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                INSERT INTO "FeatureNotifications" ("Type", "Title", "Summary", "IssueUrl", "CreatedAt")
                VALUES (
                    'BugFix',
                    'Sidebar navigation highlighting',
                    'The sidebar now correctly highlights the current section when viewing note details or other child pages.',
                    'https://github.com/garethbaumgart/praxis-note/issues/218',
                    '2026-01-27T09:01:00Z'
                );
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM "FeatureNotifications"
                WHERE "Title" = 'Sidebar navigation highlighting'
                  AND "IssueUrl" = 'https://github.com/garethbaumgart/praxis-note/issues/218';
                """);
        }
    }
}
