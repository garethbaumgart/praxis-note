using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PraxisNote.Infrastructure.Migrations.Data.FeatureNotifications
{
    public partial class AddNotificationDynamicGreetings : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                INSERT INTO "FeatureNotifications" ("Type", "Title", "Summary", "IssueUrl", "CreatedAt")
                VALUES (
                    'Feature',
                    'Dynamic personalized greetings',
                    'The home page greeting now rotates through 25+ variants based on time of day, day of week, and how long since your last visit.',
                    'https://github.com/garethbaumgart/praxis-note/issues/418',
                    '2026-02-07T11:58:07Z'
                );
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM "FeatureNotifications"
                WHERE "Title" = 'Dynamic personalized greetings';
                """);
        }
    }
}
