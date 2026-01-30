using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PraxisNote.Infrastructure.Migrations.Data.FeatureNotifications
{
    public partial class AddNotificationSelfReflectionPrompts : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                INSERT INTO "FeatureNotifications" ("Type", "Title", "Summary", "IssueUrl", "CreatedAt")
                VALUES (
                    'Feature',
                    'Post-meeting self-reflection',
                    'Reflect on your meeting behavior with contextual prompts generated from AI analysis. Compare your self-assessment to actual data for awareness insights.',
                    'https://github.com/garethbaumgart/praxis-note/issues/281',
                    '2026-01-30T12:00:00Z'
                );
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM "FeatureNotifications"
                WHERE "Title" = 'Post-meeting self-reflection';
                """);
        }
    }
}
