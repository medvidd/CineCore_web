using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CineCoreBack.Migrations
{
    /// <inheritdoc />
    public partial class AddLocAndPropEnumFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "acquisition_type",
                table: "props",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "prop_status",
                table: "props",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "prop_type",
                table: "props",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "location_type",
                table: "locations",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "acquisition_type",
                table: "props");

            migrationBuilder.DropColumn(
                name: "prop_status",
                table: "props");

            migrationBuilder.DropColumn(
                name: "prop_type",
                table: "props");

            migrationBuilder.DropColumn(
                name: "location_type",
                table: "locations");
        }
    }
}
