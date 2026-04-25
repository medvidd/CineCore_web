using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CineCoreBack.Migrations
{
    /// <inheritdoc />
    public partial class AddScriptLogicFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "notes",
                table: "scenes",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_auto_generated",
                table: "roles",
                type: "boolean",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "notes",
                table: "scenes");

            migrationBuilder.DropColumn(
                name: "is_auto_generated",
                table: "roles");
        }
    }
}
