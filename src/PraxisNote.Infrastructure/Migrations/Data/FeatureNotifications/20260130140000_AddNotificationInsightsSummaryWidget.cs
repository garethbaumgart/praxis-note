using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PraxisNote.Infrastructure.Migrations.Data.FeatureNotifications
{
    public partial class AddNotificationInsightsSummaryWidget : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                INSERT INTO "FeatureNotifications" ("Type", "Title", "Summary", "IssueUrl", "CreatedAt")
                VALUES (
                    'Feature',
                    'Insights on your home page',
                    'See your talk-time trend, question ratio, and red flag count at a glance on the home page. Click through to the full insights dashboard.',
                    'https://github.com/garethbaumgart/praxis-note/pull/290',
                    '2026-01-30T14:00:00Z'
                );
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM "FeatureNotifications"
                WHERE "Title" = 'Insights on your home page';
                """);
        }
    }
}
