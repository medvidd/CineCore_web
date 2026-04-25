using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CineCoreBack.Migrations
{
    /// <inheritdoc />
    public partial class SimplifyActorModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "actors_email_key",
                table: "actors");

            migrationBuilder.DropIndex(
                name: "actors_phone_num_key",
                table: "actors");

            migrationBuilder.DropColumn(
                name: "birth_date",
                table: "actors");

            migrationBuilder.DropColumn(
                name: "email",
                table: "actors");

            migrationBuilder.DropColumn(
                name: "first_name",
                table: "actors");

            migrationBuilder.DropColumn(
                name: "last_name",
                table: "actors");

            migrationBuilder.DropColumn(
                name: "phone_num",
                table: "actors");

            migrationBuilder.AlterColumn<int>(
                name: "id",
                table: "actors",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer")
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AddForeignKey(
                name: "actors_user_id_fkey",
                table: "actors",
                column: "id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "actors_user_id_fkey",
                table: "actors");

            migrationBuilder.AlterColumn<int>(
                name: "id",
                table: "actors",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer")
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AddColumn<DateOnly>(
                name: "birth_date",
                table: "actors",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "email",
                table: "actors",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "first_name",
                table: "actors",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "last_name",
                table: "actors",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "phone_num",
                table: "actors",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "actors_email_key",
                table: "actors",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "actors_phone_num_key",
                table: "actors",
                column: "phone_num",
                unique: true);
        }
    }
}
