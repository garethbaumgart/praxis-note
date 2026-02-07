using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PraxisNote.Infrastructure.Migrations.Data.FeatureNotifications
{
    public partial class AddNotificationConsistentEmptyStates : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                INSERT INTO "FeatureNotifications" ("Type", "Title", "Summary", "IssueUrl", "CreatedAt")
                VALUES (
                    'Improvement',
                    'Consistent empty states',
                    'Empty screens across the app now follow a unified pattern with clear icons, titles, and descriptions for a more polished experience.',
                    'https://github.com/garethbaumgart/praxis-note/issues/413',
                    '2026-02-07T15:00:00Z'
                );
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM "FeatureNotifications"
                WHERE "Title" = 'Consistent empty states'
                  AND "Type" = 'Improvement'
                  AND "CreatedAt" = '2026-02-07T15:00:00Z';
                """);
        }
    }
}
