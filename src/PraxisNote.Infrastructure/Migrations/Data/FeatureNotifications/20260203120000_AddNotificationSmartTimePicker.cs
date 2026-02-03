using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PraxisNote.Infrastructure.Migrations.Data.FeatureNotifications
{
    public partial class AddNotificationSmartTimePicker : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                INSERT INTO "FeatureNotifications" ("Type", "Title", "Summary", "IssueUrl", "CreatedAt")
                VALUES (
                    'Improvement',
                    'Smart meeting time picker',
                    'Type or pick meeting times with a single combobox. Supports flexible formats like 630pm or 6:30 PM, and defaults to the nearest 30-minute interval.',
                    'https://github.com/garethbaumgart/praxis-note/pull/306',
                    '2026-02-03T12:00:00Z'
                );
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM "FeatureNotifications"
                WHERE "Title" = 'Smart meeting time picker';
                """);
        }
    }
}
