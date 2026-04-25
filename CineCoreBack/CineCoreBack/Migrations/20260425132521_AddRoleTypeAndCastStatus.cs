using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CineCoreBack.Migrations
{
    /// <inheritdoc />
    public partial class AddRoleTypeAndCastStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "role_type",
                table: "roles",
                type: "text",
                nullable: false,
                defaultValueSql: "'supporting'::enm_role_type");

            migrationBuilder.AddColumn<string>(
                name: "cast_status",
                table: "casting",
                type: "text",
                nullable: false,
                defaultValueSql: "'pending'::enm_cast_status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "role_type",
                table: "roles");

            migrationBuilder.DropColumn(
                name: "cast_status",
                table: "casting");
        }
    }
}
