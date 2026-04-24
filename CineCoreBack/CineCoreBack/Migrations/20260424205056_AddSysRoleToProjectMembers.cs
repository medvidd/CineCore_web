using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CineCoreBack.Migrations
{
    /// <inheritdoc />
    public partial class AddSysRoleToProjectMembers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "sys_role",
                table: "project_members",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "manager");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "sys_role",
                table: "project_members");
        }
    }
}
