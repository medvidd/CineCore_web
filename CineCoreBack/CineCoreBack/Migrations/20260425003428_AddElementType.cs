using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CineCoreBack.Migrations
{
    /// <inheritdoc />
    public partial class AddElementType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "element_type",
                table: "script_elements",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "element_type",
                table: "script_elements");
        }
    }
}
