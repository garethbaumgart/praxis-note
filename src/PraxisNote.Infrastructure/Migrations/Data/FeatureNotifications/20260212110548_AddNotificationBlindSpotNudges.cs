using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PraxisNote.Infrastructure.Migrations.Data.FeatureNotifications
{
    public partial class AddNotificationBlindSpotNudges : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                INSERT INTO "FeatureNotifications" ("Type", "Title", "Summary", "IssueUrl", "CreatedAt")
                VALUES (
                    'Feature',
                    'Blind spot nudges',
                    'Get AI-powered coaching tips based on gaps between your self-assessment and meeting analysis. Convert nudges into tracked goals or dismiss them.',
                    'https://github.com/garethbaumgart/praxis-note/issues/298',
                    '2026-02-12T11:05:48Z'
                );
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM "FeatureNotifications"
                WHERE "Title" = 'Blind spot nudges';
                """);
        }
    }
}
