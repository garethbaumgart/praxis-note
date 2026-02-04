using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PraxisNote.Infrastructure.Migrations.Data.FeatureNotifications
{
    public partial class AddNotificationScreenshotImport : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                INSERT INTO "FeatureNotifications" ("Type", "Title", "Summary", "IssueUrl", "CreatedAt")
                VALUES (
                    'Feature',
                    'Import meetings from screenshots',
                    'Paste or drop a screenshot of your calendar to instantly extract and import meetings. Supports PNG, JPG, and WebP.',
                    'https://github.com/garethbaumgart/praxis-note/issues/317',
                    '2026-02-04T12:03:00Z'
                );
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM "FeatureNotifications"
                WHERE "Title" = 'Import meetings from screenshots';
                """);
        }
    }
}
