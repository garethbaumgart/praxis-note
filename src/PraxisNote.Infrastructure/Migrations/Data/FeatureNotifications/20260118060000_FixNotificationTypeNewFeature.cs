using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PraxisNote.Infrastructure.Migrations.Data.FeatureNotifications
{
    /// <inheritdoc />
    public partial class FixNotificationTypeNewFeature : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Fix incorrect 'NewFeature' values to 'Feature'
            // The NotificationType enum only has: Feature, BugFix, Improvement
            migrationBuilder.Sql("""
                UPDATE "FeatureNotifications"
                SET "Type" = 'Feature'
                WHERE "Type" = 'NewFeature';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No-op: we don't want to revert to the broken state
        }
    }
}
