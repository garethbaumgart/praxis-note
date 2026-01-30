using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PraxisNote.Infrastructure.Migrations.Data.FeatureNotifications
{
    public partial class AddNotificationNoteTagManagement : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                INSERT INTO "FeatureNotifications" ("Type", "Title", "Summary", "IssueUrl", "CreatedAt")
                VALUES (
                    'Improvement',
                    'Note tag management and filtering',
                    'Tag notes directly from cards on hover, see a visible "Add tag" button in the editor, and filter your notes grid by tag.',
                    'https://github.com/garethbaumgart/praxis-note/pull/285',
                    '2026-01-30T11:01:20Z'
                );
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM "FeatureNotifications"
                WHERE "Title" = 'Note tag management and filtering';
                """);
        }
    }
}
