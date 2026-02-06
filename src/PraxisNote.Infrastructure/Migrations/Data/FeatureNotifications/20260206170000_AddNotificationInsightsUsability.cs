using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PraxisNote.Infrastructure.Migrations.Data.FeatureNotifications
{
    public partial class AddNotificationInsightsUsability : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                INSERT INTO "FeatureNotifications" ("Type", "Title", "Summary", "IssueUrl", "CreatedAt")
                VALUES (
                    'BugFix',
                    'Insights section improvements',
                    'Added info tooltips to Goals, Communication Style, and Johari Window sections. Fixed behavioral trends showing the wrong participant by default.',
                    'https://github.com/garethbaumgart/praxis-note/issues/356',
                    '2026-02-06T17:00:00Z'
                );
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM "FeatureNotifications"
                WHERE "Title" = 'Insights section improvements';
                """);
        }
    }
}
