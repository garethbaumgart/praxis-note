using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PraxisNote.Infrastructure.Migrations.Data.FeatureNotifications
{
    public partial class AddNotificationSlashCommands : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                INSERT INTO "FeatureNotifications" ("Type", "Title", "Summary", "IssueUrl", "CreatedAt")
                VALUES (
                    'Feature',
                    'Slash commands in note editor',
                    'Type / in the note editor to open a searchable command menu for headings, lists, blocks, tables, and more. Use Cmd+Shift+D to insert today''s date inline.',
                    'https://github.com/garethbaumgart/praxis-note/issues/494',
                    '2026-02-13T04:45:00+00:00'
                );
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM "FeatureNotifications"
                WHERE "Title" = 'Slash commands in note editor';
                """);
        }
    }
}
