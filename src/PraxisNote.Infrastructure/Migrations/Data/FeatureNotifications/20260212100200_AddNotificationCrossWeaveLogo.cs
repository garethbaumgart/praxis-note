using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PraxisNote.Infrastructure.Migrations.Data.FeatureNotifications
{
    public partial class AddNotificationCrossWeaveLogo : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                INSERT INTO "FeatureNotifications" ("Type", "Title", "Summary", "IssueUrl", "CreatedAt")
                VALUES (
                    'Improvement',
                    'New Cross Weave logo',
                    'Fresh logo with interlocking woven bars in Nord aurora colors and a dark squircle checkmark badge.',
                    'https://github.com/garethbaumgart/praxis-note/pull/488',
                    '2026-02-12T10:02:00Z'
                );
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM "FeatureNotifications"
                WHERE "Title" = 'New Cross Weave logo';
                """);
        }
    }
}
