using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PraxisNote.Infrastructure.Migrations.Data.FeatureNotifications
{
    public partial class AddNotificationClickableHomeTasks : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                INSERT INTO "FeatureNotifications" ("Type", "Title", "Summary", "IssueUrl", "CreatedAt")
                VALUES (
                    'Improvement',
                    'Clickable tasks on the Home page',
                    'Click any task in the Home page My Tasks widget to jump directly to it on the Tasks board with a highlight effect.',
                    'https://github.com/garethbaumgart/praxis-note/issues/470',
                    '2026-02-11T10:31:00Z'
                );
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM "FeatureNotifications"
                WHERE "Title" = 'Clickable tasks on the Home page';
                """);
        }
    }
}
