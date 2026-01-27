using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PraxisNote.Infrastructure.Migrations.Data.FeatureNotifications
{
    public partial class AddNotificationCheckboxPromotionFix : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                INSERT INTO "FeatureNotifications" ("Type", "Title", "Summary", "IssueUrl", "CreatedAt")
                VALUES (
                    'BugFix',
                    'Checkbox promotion reliability',
                    'Fixed an issue where promoting checkboxes to tasks could fail under heavy load. Checkbox promotion now works reliably every time.',
                    'https://github.com/garethbaumgart/praxis-note/issues/226',
                    '2026-01-27T09:02:00Z'
                );
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM "FeatureNotifications"
                WHERE "Title" = 'Checkbox promotion reliability'
                  AND "IssueUrl" = 'https://github.com/garethbaumgart/praxis-note/issues/226';
                """);
        }
    }
}
