using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PraxisNote.Infrastructure.Migrations.Data.FeatureNotifications
{
    public partial class AddNotificationDriveBulkImportPreview : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                INSERT INTO "FeatureNotifications" ("Type", "Title", "Summary", "IssueUrl", "CreatedAt")
                VALUES (
                    'Feature',
                    'Drive bulk import preview',
                    'Preview parsed Drive files before importing, with duplicate detection, inline tag editing, and batch confirmation with progress tracking.',
                    'https://github.com/garethbaumgart/praxis-note/pull/667',
                    '2026-02-26T21:40:00Z'
                );
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM "FeatureNotifications"
                WHERE "Title" = 'Drive bulk import preview';
                """);
        }
    }
}
