using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PraxisNote.Infrastructure.Migrations.Data.FeatureNotifications
{
    /// <inheritdoc />
    public partial class AddMissingNotifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add notifications that were missing from initial seed
            migrationBuilder.Sql("""
                INSERT INTO "FeatureNotifications" ("Type", "Title", "Summary", "IssueUrl", "CreatedAt")
                VALUES
                    ('Improvement', 'Smarter task sorting', 'Priority and due date sorts now use each other as secondary sorts for smarter task ordering.', 'https://github.com/garethbaumgart/praxis-note/pull/125', '2026-01-16T00:00:00Z'),
                    ('BugFix', 'Equal column heights on desktop', 'Kanban columns now maintain equal heights on desktop for a cleaner, more consistent board layout.', 'https://github.com/garethbaumgart/praxis-note/pull/126', '2026-01-16T01:00:00Z');
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM "FeatureNotifications"
                WHERE "Title" IN ('Smarter task sorting', 'Equal column heights on desktop');
                """);
        }
    }
}
