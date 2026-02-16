using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PraxisNote.Infrastructure.Migrations.Data.FeatureNotifications
{
    public partial class AddNotificationSmartPdfPaste : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                INSERT INTO "FeatureNotifications" ("Type", "Title", "Summary", "IssueUrl", "CreatedAt")
                VALUES (
                    'BugFix',
                    'Smarter PDF paste formatting',
                    'Pasting text from PDFs now preserves bullet lists, numbered lists, and headings instead of flattening everything into plain paragraphs.',
                    'https://github.com/garethbaumgart/praxis-note/pull/557',
                    '2026-02-16T11:48:00Z'
                );
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM "FeatureNotifications"
                WHERE "Title" = 'Smarter PDF paste formatting';
                """);
        }
    }
}
