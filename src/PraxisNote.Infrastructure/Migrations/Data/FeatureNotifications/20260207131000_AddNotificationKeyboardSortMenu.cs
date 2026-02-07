using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PraxisNote.Infrastructure.Migrations.Data.FeatureNotifications
{
    public partial class AddNotificationKeyboardSortMenu : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                INSERT INTO "FeatureNotifications" ("Type", "Title", "Summary", "IssueUrl", "CreatedAt")
                VALUES (
                    'Improvement',
                    'Keyboard-accessible sort menu',
                    'The column sort dropdown now supports arrow-key navigation, Escape to close, and proper screen reader announcements.',
                    'https://github.com/garethbaumgart/praxis-note/issues/421',
                    '2026-02-07T13:10:00Z'
                );
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM "FeatureNotifications"
                WHERE "Title" = 'Keyboard-accessible sort menu';
                """);
        }
    }
}
