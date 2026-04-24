using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CineCoreBack.Migrations
{
    /// <inheritdoc />
    public partial class AddProjectIdToResource : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "project_id",
                table: "resources",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_resources_project_id",
                table: "resources",
                column: "project_id");

            migrationBuilder.AddForeignKey(
                name: "resources_project_id_fkey",
                table: "resources",
                column: "project_id",
                principalTable: "projects",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "resources_project_id_fkey",
                table: "resources");

            migrationBuilder.DropIndex(
                name: "IX_resources_project_id",
                table: "resources");

            migrationBuilder.DropColumn(
                name: "project_id",
                table: "resources");
        }
    }
}
