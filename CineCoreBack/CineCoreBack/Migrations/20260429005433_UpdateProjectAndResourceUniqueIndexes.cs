using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CineCoreBack.Migrations
{
    /// <inheritdoc />
    public partial class UpdateProjectAndResourceUniqueIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "props_prop_name_key",
                table: "props");

            migrationBuilder.DropIndex(
                name: "IX_projects_owner_id",
                table: "projects");

            migrationBuilder.DropIndex(
                name: "projects_title_key",
                table: "projects");

            migrationBuilder.DropIndex(
                name: "locations_location_name_key",
                table: "locations");

            migrationBuilder.CreateIndex(
                name: "uq_projects_owner_title",
                table: "projects",
                columns: new[] { "owner_id", "title" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "uq_projects_owner_title",
                table: "projects");

            migrationBuilder.CreateIndex(
                name: "props_prop_name_key",
                table: "props",
                column: "prop_name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_projects_owner_id",
                table: "projects",
                column: "owner_id");

            migrationBuilder.CreateIndex(
                name: "projects_title_key",
                table: "projects",
                column: "title",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "locations_location_name_key",
                table: "locations",
                column: "location_name",
                unique: true);
        }
    }
}
