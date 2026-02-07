using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PraxisNote.Infrastructure.Migrations.Data.FeatureNotifications
{
    public partial class AddNotificationStandardizedLoadingStates : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                INSERT INTO "FeatureNotifications" ("Type", "Title", "Summary", "IssueUrl", "CreatedAt")
                VALUES (
                    'Improvement',
                    'Polished loading states',
                    'Loading indicators now use consistent skeleton animations across all pages, with improved accessibility for screen readers.',
                    'https://github.com/garethbaumgart/praxis-note/issues/414',
                    '2026-02-07T10:57:00Z'
                );
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM "FeatureNotifications"
                WHERE "Title" = 'Polished loading states'
                  AND "IssueUrl" = 'https://github.com/garethbaumgart/praxis-note/issues/414'
                  AND "CreatedAt" = '2026-02-07T10:57:00Z';
                """);
        }
    }
}
