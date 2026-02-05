using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PraxisNote.Infrastructure.Migrations.Data.FeatureNotifications
{
    public partial class AddNotificationExcludeFromInsights : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                INSERT INTO "FeatureNotifications" ("Type", "Title", "Summary", "IssueUrl", "CreatedAt")
                VALUES (
                    'Feature',
                    'Exclude meetings from insights',
                    'You can now exclude specific meetings from your behavioral insights and communication profile, keeping your analytics focused on relevant data.',
                    'https://github.com/garethbaumgart/praxis-note/issues/327',
                    '2026-02-05T09:02:00Z'
                );
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM "FeatureNotifications"
                WHERE "Title" = 'Exclude meetings from insights';
                """);
        }
    }
}
