using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PraxisNote.Infrastructure.Migrations.Data.FeatureNotifications
{
    public partial class AddNotificationStickyEditorToolbar : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                INSERT INTO "FeatureNotifications" ("Type", "Title", "Summary", "IssueUrl", "CreatedAt")
                VALUES (
                    'Improvement',
                    'Sticky note editor toolbar',
                    'The formatting toolbar now stays pinned at the top of the editor as you scroll through long notes, keeping all controls within easy reach.',
                    'https://github.com/garethbaumgart/praxis-note/pull/302',
                    '2026-02-02T12:00:00Z'
                );
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM "FeatureNotifications"
                WHERE "Title" = 'Sticky note editor toolbar';
                """);
        }
    }
}
