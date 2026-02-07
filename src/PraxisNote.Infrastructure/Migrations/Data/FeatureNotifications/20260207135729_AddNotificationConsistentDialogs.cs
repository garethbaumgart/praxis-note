using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PraxisNote.Infrastructure.Migrations.Data.FeatureNotifications
{
    public partial class AddNotificationConsistentDialogs : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                INSERT INTO "FeatureNotifications" ("Type", "Title", "Summary", "IssueUrl", "CreatedAt")
                VALUES (
                    'Improvement',
                    'Consistent dialog experience',
                    'All dialogs now share uniform sizing, button styles, and click-outside-to-close behavior for a smoother, more predictable experience.',
                    'https://github.com/garethbaumgart/praxis-note/issues/423',
                    '2026-02-07T13:57:29Z'
                );
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM "FeatureNotifications"
                WHERE "Title" = 'Consistent dialog experience';
                """);
        }
    }
}
