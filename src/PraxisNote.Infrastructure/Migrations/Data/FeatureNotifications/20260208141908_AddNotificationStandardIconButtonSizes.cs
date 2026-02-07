using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PraxisNote.Infrastructure.Migrations.Data.FeatureNotifications
{
    public partial class AddNotificationStandardIconButtonSizes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                INSERT INTO "FeatureNotifications" ("Type", "Title", "Summary", "IssueUrl", "CreatedAt")
                VALUES (
                    'Improvement',
                    'Consistent icon button sizes',
                    'Icon buttons now follow a standard 3-tier sizing system (sm/md/lg) for a cleaner look and easier touch targets on mobile.',
                    'https://github.com/garethbaumgart/praxis-note/issues/425',
                    '2026-02-08T14:19:08Z'
                );
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM "FeatureNotifications"
                WHERE "Title" = 'Consistent icon button sizes';
                """);
        }
    }
}
