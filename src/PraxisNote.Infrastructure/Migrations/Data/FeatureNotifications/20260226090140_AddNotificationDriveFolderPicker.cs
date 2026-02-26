using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PraxisNote.Infrastructure.Migrations.Data.FeatureNotifications
{
    public partial class AddNotificationDriveFolderPicker : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                INSERT INTO "FeatureNotifications" ("Type", "Title", "Summary", "IssueUrl", "CreatedAt")
                VALUES (
                    'Feature',
                    'Drive folder picker & sync config',
                    'After connecting Google Drive, select a folder to sync and configure import settings including cutoff date, sync frequency, and auto-accept tags.',
                    'https://github.com/garethbaumgart/praxis-note/issues/649',
                    '2026-02-26T09:01:40Z'
                );
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM "FeatureNotifications"
                WHERE "Title" = 'Drive folder picker & sync config';
                """);
        }
    }
}
