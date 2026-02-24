using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PraxisNote.Infrastructure.Migrations.Data.FeatureNotifications
{
    public partial class AddNotificationAutoSuggestTags : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                INSERT INTO "FeatureNotifications" ("Type", "Title", "Summary", "IssueUrl", "CreatedAt")
                VALUES (
                    'Feature',
                    'Auto-suggest tags for transcript imports',
                    'Tags are now automatically suggested when importing meetings from transcripts. 1:1 meetings suggest the other person''s name. Review, add, or remove tags before confirming the import.',
                    'https://github.com/garethbaumgart/praxis-note/issues/645',
                    '2026-02-24T10:45:00Z'
                );
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM "FeatureNotifications"
                WHERE "Title" = 'Auto-suggest tags for transcript imports';
                """);
        }
    }
}
