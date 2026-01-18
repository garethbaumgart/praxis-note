using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PraxisNote.Infrastructure.Migrations.Data.FeatureNotifications
{
    /// <inheritdoc />
    public partial class AddNotificationCommentImprovements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                INSERT INTO "FeatureNotifications" ("Type", "Title", "Summary", "IssueUrl", "CreatedAt")
                VALUES (
                    'Improvement',
                    'Comment delete improvements',
                    'Comment delete button is now visible on mobile. Accidentally deleted a comment? Click Undo in the toast to restore it.',
                    'https://github.com/garethbaumgart/praxis-note/issues/140',
                    '2026-01-18T03:30:00Z'
                );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM "FeatureNotifications"
                WHERE "Title" = 'Comment delete improvements'
                  AND "CreatedAt" = '2026-01-18T03:30:00Z';
                """);
        }
    }
}
