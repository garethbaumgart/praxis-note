using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PraxisNote.Infrastructure.Migrations.Data.FeatureNotifications
{
    public partial class AddNotificationCompactTrendCharts : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                INSERT INTO "FeatureNotifications" ("Type", "Title", "Summary", "IssueUrl", "CreatedAt")
                VALUES (
                    'Improvement',
                    'Compact insights dashboard',
                    'Behavioral trend charts now display in a compact 2-column grid with sparklines, current values, and trend indicators for quicker scanning.',
                    'https://github.com/garethbaumgart/praxis-note/issues/316',
                    '2026-02-04T12:02:00Z'
                );
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM "FeatureNotifications"
                WHERE "Title" = 'Compact insights dashboard';
                """);
        }
    }
}
