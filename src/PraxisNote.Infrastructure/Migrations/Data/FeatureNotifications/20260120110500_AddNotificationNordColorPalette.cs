using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PraxisNote.Infrastructure.Migrations.Data.FeatureNotifications
{
    public partial class AddNotificationNordColorPalette : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                INSERT INTO "FeatureNotifications" ("Type", "Title", "Summary", "IssueUrl", "CreatedAt")
                VALUES (
                    'Improvement',
                    'Nord-inspired color palette',
                    'Fresh new look with harmonious colors inspired by the Nord palette for both light and dark modes.',
                    'https://github.com/garethbaumgart/praxis-note/issues/189',
                    '2026-01-20T11:05:00Z'
                );
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM "FeatureNotifications"
                WHERE "Title" = 'Nord-inspired color palette';
                """);
        }
    }
}
