using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PraxisNote.Infrastructure.Migrations.Data.FeatureNotifications
{
    public partial class AddNotificationSkipToMainContent : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                INSERT INTO "FeatureNotifications" ("Type", "Title", "Summary", "IssueUrl", "CreatedAt")
                VALUES (
                    'Improvement',
                    'Skip to main content',
                    'Keyboard users can now press Tab to reveal a skip link that jumps directly to the main content area, bypassing the sidebar navigation.',
                    'https://github.com/garethbaumgart/praxis-note/issues/416',
                    '2026-02-07T11:40:31Z'
                );
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM "FeatureNotifications"
                WHERE "Title" = 'Skip to main content';
                """);
        }
    }
}
