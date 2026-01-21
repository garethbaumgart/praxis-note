using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PraxisNote.Infrastructure.Migrations.Data.FeatureNotifications
{
    public partial class AddNotificationTaskTags : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                INSERT INTO "FeatureNotifications" ("Type", "Title", "Summary", "IssueUrl", "CreatedAt")
                VALUES (
                    'Feature',
                    'Add tags to tasks',
                    'Organize your tasks with custom colored tags - create, assign, and manage tags directly from task cards.',
                    'https://github.com/garethbaumgart/praxis-note/issues/197',
                    '2026-01-21T12:00:00Z'
                );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM "FeatureNotifications"
                WHERE "Title" = 'Add tags to tasks'
                  AND "CreatedAt" = '2026-01-21T12:00:00Z';
                """);
        }
    }
}
