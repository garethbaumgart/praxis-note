using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PraxisNote.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddNotifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LastSeenNotificationId",
                table: "Users",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "FeatureNotifications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Summary = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    IssueUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeatureNotifications", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FeatureNotifications_CreatedAt",
                table: "FeatureNotifications",
                column: "CreatedAt");

            // Seed initial notifications (10 recent features)
            migrationBuilder.Sql("""
                INSERT INTO "FeatureNotifications" ("Type", "Title", "Summary", "IssueUrl", "CreatedAt")
                VALUES
                    ('Feature', 'Archive view for Done tasks', 'Access older completed tasks in a dedicated Archive view, keeping your Done column focused on recent work.', 'https://github.com/garethbaumgart/praxis-note/pull/79', '2026-01-06T00:00:00Z'),
                    ('Improvement', 'Faster task archiving', 'Done tasks now archive after 2 days instead of 7, keeping your board cleaner.', 'https://github.com/garethbaumgart/praxis-note/pull/80', '2026-01-07T00:00:00Z'),
                    ('Feature', 'Clickable URLs in comments', 'URLs in task comments are now automatically converted to clickable links.', 'https://github.com/garethbaumgart/praxis-note/pull/88', '2026-01-08T00:00:00Z'),
                    ('Feature', 'Expandable comments', 'Comments now collapse with a badge showing the count. Click to expand and view all comments.', 'https://github.com/garethbaumgart/praxis-note/pull/89', '2026-01-09T00:00:00Z'),
                    ('Feature', 'Task search and filtering', 'Added search bar to quickly find tasks by title or description across all columns.', 'https://github.com/garethbaumgart/praxis-note/pull/91', '2026-01-10T00:00:00Z'),
                    ('Improvement', '+35 days due date shortcut', 'Quickly set due dates 5 weeks out with the new +35 days button in the date picker.', 'https://github.com/garethbaumgart/praxis-note/pull/100', '2026-01-11T00:00:00Z'),
                    ('Feature', 'Sort tasks by column', 'Sort tasks within each column by date created, due date, or priority using the new dropdown.', 'https://github.com/garethbaumgart/praxis-note/pull/101', '2026-01-12T00:00:00Z'),
                    ('Improvement', 'Skeleton loading', 'Task columns now show elegant skeleton placeholders while loading for a smoother experience.', 'https://github.com/garethbaumgart/praxis-note/pull/104', '2026-01-13T00:00:00Z'),
                    ('Feature', 'Priority flag for tasks', 'Mark important tasks with a priority flag to keep them at the top of your columns.', 'https://github.com/garethbaumgart/praxis-note/pull/114', '2026-01-14T00:00:00Z'),
                    ('Feature', 'What''s New notifications', 'Stay updated on new features and improvements with the notification bell in the header.', 'https://github.com/garethbaumgart/praxis-note/pull/118', '2026-01-15T11:00:00Z');
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FeatureNotifications");

            migrationBuilder.DropColumn(
                name: "LastSeenNotificationId",
                table: "Users");
        }
    }
}
