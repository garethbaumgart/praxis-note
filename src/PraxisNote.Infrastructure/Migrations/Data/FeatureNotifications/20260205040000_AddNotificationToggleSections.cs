using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PraxisNote.Infrastructure.Migrations.Data.FeatureNotifications
{
    public partial class AddNotificationToggleSections : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                INSERT INTO "FeatureNotifications" ("Type", "Title", "Summary", "IssueUrl", "CreatedAt")
                VALUES (
                    'Feature',
                    'Collapsible toggle sections in notes',
                    'Organize long notes with collapsible sections. Use the Toggle Section button in the toolbar menu to wrap content in expandable blocks.',
                    'https://github.com/garethbaumgart/praxis-note/pull/326',
                    '2026-02-05T04:00:00Z'
                );
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM "FeatureNotifications"
                WHERE "Title" = 'Collapsible toggle sections in notes';
                """);
        }
    }
}
