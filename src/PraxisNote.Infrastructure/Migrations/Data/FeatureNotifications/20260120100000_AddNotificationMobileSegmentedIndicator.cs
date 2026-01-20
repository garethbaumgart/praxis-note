using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PraxisNote.Infrastructure.Migrations.Data.FeatureNotifications
{
    /// <inheritdoc />
    public partial class AddNotificationMobileSegmentedIndicator : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                INSERT INTO "FeatureNotifications" ("Type", "Title", "Summary", "IssueUrl", "CreatedAt")
                VALUES (
                    'Improvement',
                    'Color-coded mobile column indicator',
                    'The mobile navigation bar now shows color-coded segments matching each column''s status for clearer orientation.',
                    'https://github.com/garethbaumgart/praxis-note/issues/191',
                    '2026-01-20T10:00:00Z'
                );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM "FeatureNotifications"
                WHERE "Title" = 'Color-coded mobile column indicator'
                  AND "CreatedAt" = '2026-01-20T10:00:00Z';
                """);
        }
    }
}
