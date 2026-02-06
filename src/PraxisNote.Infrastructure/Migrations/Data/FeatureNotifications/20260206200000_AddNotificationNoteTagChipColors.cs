using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PraxisNote.Infrastructure.Migrations.Data.FeatureNotifications
{
    public partial class AddNotificationNoteTagChipColors : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                INSERT INTO "FeatureNotifications" ("Type", "Title", "Summary", "IssueUrl", "CreatedAt")
                VALUES (
                    'Improvement',
                    'Consistent tag filter chip colors',
                    'Tag filter chips on the Notes page now use the app''s green tag color palette instead of neutral gray, making them instantly recognisable as tags.',
                    'https://github.com/garethbaumgart/praxis-note/issues/358',
                    '2026-02-06T20:00:00Z'
                );
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM "FeatureNotifications"
                WHERE "Title" = 'Consistent tag filter chip colors';
                """);
        }
    }
}
