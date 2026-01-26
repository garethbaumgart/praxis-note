using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PraxisNote.Infrastructure.Migrations.Data.FeatureNotifications
{
    public partial class AddNotificationNotesAndCheckboxSync : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                INSERT INTO "FeatureNotifications" ("Type", "Title", "Summary", "IssueUrl", "CreatedAt")
                VALUES (
                    'Feature',
                    'Notes with checkbox-task sync',
                    'Create rich text notes with a TipTap editor. Promote checkboxes to tasks with automatic bidirectional sync.',
                    'https://github.com/garethbaumgart/praxis-note/pull/206',
                    '2026-01-24T15:00:00Z'
                );
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM "FeatureNotifications"
                WHERE "Title" = 'Notes with checkbox-task sync';
                """);
        }
    }
}
