using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PraxisNote.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMeetingTagsAndActionItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TagIds",
                table: "Meetings",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ActionItems",
                table: "Meetings",
                type: "jsonb",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TagIds",
                table: "Meetings");

            migrationBuilder.DropColumn(
                name: "ActionItems",
                table: "Meetings");
        }
    }
}
