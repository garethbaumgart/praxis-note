using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PraxisNote.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RenameLabelIdsToTagIds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "LabelIds",
                table: "Tasks",
                newName: "TagIds");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "TagIds",
                table: "Tasks",
                newName: "LabelIds");
        }
    }
}
