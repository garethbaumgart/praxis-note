using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PraxisNote.Infrastructure.Migrations.Data.FeatureNotifications
{
    public partial class AddNotificationInlineCheckboxPromotion : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                INSERT INTO "FeatureNotifications" ("Type", "Title", "Summary", "IssueUrl", "CreatedAt")
                VALUES (
                    'Improvement',
                    'Inline checkbox promotion',
                    'Hover over any checkbox in a note to reveal a quick promote button. Linked checkboxes show their task status inline.',
                    'https://github.com/garethbaumgart/praxis-note/pull/214',
                    '2026-01-26T19:25:00Z'
                );
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM "FeatureNotifications"
                WHERE "Title" = 'Inline checkbox promotion'
                  AND "IssueUrl" = 'https://github.com/garethbaumgart/praxis-note/pull/214';
                """);
        }
    }
}
