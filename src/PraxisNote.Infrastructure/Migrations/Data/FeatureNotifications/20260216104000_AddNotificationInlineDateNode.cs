using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PraxisNote.Infrastructure.Migrations.Data.FeatureNotifications
{
    public partial class AddNotificationInlineDateNode : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                INSERT INTO "FeatureNotifications" ("Type", "Title", "Summary", "IssueUrl", "CreatedAt")
                VALUES (
                    'Feature',
                    'Inline date chips',
                    'Insert styled date chips with /date, /today, or /tomorrow. Click any date chip to change it with quick-pick buttons or a date picker.',
                    'https://github.com/garethbaumgart/praxis-note/issues/561',
                    '2026-02-16T10:40:00Z'
                );
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM "FeatureNotifications"
                WHERE "Title" = 'Inline date chips';
                """);
        }
    }
}
