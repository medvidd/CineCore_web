using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CineCoreBack.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:enm_acquisition_type", "buy,rent")
                .Annotation("Npgsql:Enum:enm_cast_status", "pending,approved,hold,declined")
                .Annotation("Npgsql:Enum:enm_gender", "f,m,any")
                .Annotation("Npgsql:Enum:enm_location_type", "interior,exterior,studio")
                .Annotation("Npgsql:Enum:enm_member_status", "pending,active,declined")
                .Annotation("Npgsql:Enum:enm_prop_status", "available,leased,unavailable")
                .Annotation("Npgsql:Enum:enm_prop_type", "action,scenography,functional")
                .Annotation("Npgsql:Enum:enm_resource_type", "ROLE,LOCATION,PROP")
                .Annotation("Npgsql:Enum:enm_role_type", "lead,supporting,extra")
                .Annotation("Npgsql:Enum:enm_scene_status", "draft,complete")
                .Annotation("Npgsql:Enum:enm_script_element", "action,character,dialogue,parenthetical,transition,shot")
                .Annotation("Npgsql:Enum:enm_shoot_day_status", "draft,published,completed,cancelled")
                .Annotation("Npgsql:Enum:enm_system_role", "owner,manager,actor");

            migrationBuilder.CreateTable(
                name: "actors",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    first_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    last_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    birth_date = table.Column<DateOnly>(type: "date", nullable: true),
                    phone_num = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    characteristics = table.Column<string>(type: "jsonb", nullable: true, defaultValueSql: "'{}'::jsonb")
                },
                constraints: table =>
                {
                    table.PrimaryKey("actors_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "resources",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn)
                },
                constraints: table =>
                {
                    table.PrimaryKey("resources_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    password_hash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    first_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    last_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    phone_num = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    registered_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("users_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "locations",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    location_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    city = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    street = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    contact_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    contact_phone = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("locations_pkey", x => x.id);
                    table.ForeignKey(
                        name: "locations_id_fkey",
                        column: x => x.id,
                        principalTable: "resources",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "props",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    prop_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("props_pkey", x => x.id);
                    table.ForeignKey(
                        name: "props_id_fkey",
                        column: x => x.id,
                        principalTable: "resources",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "projects",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    synopsis = table.Column<string>(type: "text", nullable: true),
                    start_date = table.Column<DateOnly>(type: "date", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP"),
                    owner_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("projects_pkey", x => x.id);
                    table.ForeignKey(
                        name: "projects_owner_id_fkey",
                        column: x => x.owner_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "project_members",
                columns: table => new
                {
                    project_id = table.Column<int>(type: "integer", nullable: false),
                    invited_email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    user_id = table.Column<int>(type: "integer", nullable: true),
                    job_title = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    department = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    invited_by_user_id = table.Column<int>(type: "integer", nullable: true),
                    invited_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP"),
                    joined_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("project_members_pkey", x => new { x.project_id, x.invited_email });
                    table.ForeignKey(
                        name: "project_members_invited_by_user_id_fkey",
                        column: x => x.invited_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "project_members_project_id_fkey",
                        column: x => x.project_id,
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "project_members_user_id_fkey",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "roles",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    project_id = table.Column<int>(type: "integer", nullable: false),
                    role_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    age = table.Column<int>(type: "integer", nullable: true),
                    characteristics = table.Column<string>(type: "jsonb", nullable: true, defaultValueSql: "'{}'::jsonb"),
                    color_hex = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false, defaultValueSql: "'#333333'::character varying")
                },
                constraints: table =>
                {
                    table.PrimaryKey("roles_pkey", x => x.id);
                    table.ForeignKey(
                        name: "roles_id_fkey",
                        column: x => x.id,
                        principalTable: "resources",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "roles_project_id_fkey",
                        column: x => x.project_id,
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "scenes",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    project_id = table.Column<int>(type: "integer", nullable: false),
                    sequence_num = table.Column<int>(type: "integer", nullable: false),
                    slugline_text = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    estimated_duration = table.Column<TimeSpan>(type: "interval", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("scenes_pkey", x => x.id);
                    table.ForeignKey(
                        name: "scenes_project_id_fkey",
                        column: x => x.project_id,
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "shoot_days",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    project_id = table.Column<int>(type: "integer", nullable: false),
                    unit_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, defaultValueSql: "'Main Unit'::character varying"),
                    shift_start = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    shift_end = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    base_location_id = table.Column<int>(type: "integer", nullable: true),
                    general_notes = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("shoot_days_pkey", x => x.id);
                    table.ForeignKey(
                        name: "shoot_days_base_location_id_fkey",
                        column: x => x.base_location_id,
                        principalTable: "locations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "shoot_days_project_id_fkey",
                        column: x => x.project_id,
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "casting",
                columns: table => new
                {
                    role_id = table.Column<int>(type: "integer", nullable: false),
                    actor_id = table.Column<int>(type: "integer", nullable: false),
                    cast_date = table.Column<DateOnly>(type: "date", nullable: false, defaultValueSql: "CURRENT_DATE"),
                    notes = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("casting_pkey", x => new { x.role_id, x.actor_id });
                    table.ForeignKey(
                        name: "casting_actor_id_fkey",
                        column: x => x.actor_id,
                        principalTable: "actors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "casting_role_id_fkey",
                        column: x => x.role_id,
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "scene_resource",
                columns: table => new
                {
                    scene_id = table.Column<int>(type: "integer", nullable: false),
                    resource_id = table.Column<int>(type: "integer", nullable: false),
                    notes = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("scene_resource_pkey", x => new { x.scene_id, x.resource_id });
                    table.ForeignKey(
                        name: "scene_resource_resource_id_fkey",
                        column: x => x.resource_id,
                        principalTable: "resources",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "scene_resource_scene_id_fkey",
                        column: x => x.scene_id,
                        principalTable: "scenes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "script_elements",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    scene_id = table.Column<int>(type: "integer", nullable: false),
                    order_index = table.Column<int>(type: "integer", nullable: false),
                    content = table.Column<string>(type: "text", nullable: false),
                    role_id = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("script_elements_pkey", x => x.id);
                    table.ForeignKey(
                        name: "script_elements_role_id_fkey",
                        column: x => x.role_id,
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "script_elements_scene_id_fkey",
                        column: x => x.scene_id,
                        principalTable: "scenes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "call_sheets",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    shoot_day_id = table.Column<int>(type: "integer", nullable: false),
                    version_num = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    published_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP"),
                    published_by_user_id = table.Column<int>(type: "integer", nullable: true),
                    snapshot_data = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("call_sheets_pkey", x => x.id);
                    table.ForeignKey(
                        name: "call_sheets_published_by_user_id_fkey",
                        column: x => x.published_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "call_sheets_shoot_day_id_fkey",
                        column: x => x.shoot_day_id,
                        principalTable: "shoot_days",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "scene_schedule",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    shoot_day_id = table.Column<int>(type: "integer", nullable: false),
                    scene_id = table.Column<int>(type: "integer", nullable: false),
                    scene_order = table.Column<int>(type: "integer", nullable: false),
                    scheduled_time = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    prep_time_estimate = table.Column<TimeSpan>(type: "interval", nullable: true),
                    shoot_time_estimate = table.Column<TimeSpan>(type: "interval", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("scene_schedule_pkey", x => x.id);
                    table.ForeignKey(
                        name: "scene_schedule_scene_id_fkey",
                        column: x => x.scene_id,
                        principalTable: "scenes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "scene_schedule_shoot_day_id_fkey",
                        column: x => x.shoot_day_id,
                        principalTable: "shoot_days",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

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

            migrationBuilder.CreateIndex(
                name: "IX_call_sheets_published_by_user_id",
                table: "call_sheets",
                column: "published_by_user_id");

            migrationBuilder.CreateIndex(
                name: "unq_shoot_day_version",
                table: "call_sheets",
                columns: new[] { "shoot_day_id", "version_num" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_casting_actor_id",
                table: "casting",
                column: "actor_id");

            migrationBuilder.CreateIndex(
                name: "locations_location_name_key",
                table: "locations",
                column: "location_name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_project_members_invited_by_user_id",
                table: "project_members",
                column: "invited_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_project_members_user_id",
                table: "project_members",
                column: "user_id");

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
                name: "props_prop_name_key",
                table: "props",
                column: "prop_name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_roles_project_id",
                table: "roles",
                column: "project_id");

            migrationBuilder.CreateIndex(
                name: "IX_scene_resource_resource_id",
                table: "scene_resource",
                column: "resource_id");

            migrationBuilder.CreateIndex(
                name: "IX_scene_schedule_scene_id",
                table: "scene_schedule",
                column: "scene_id");

            migrationBuilder.CreateIndex(
                name: "unq_scene_in_day",
                table: "scene_schedule",
                columns: new[] { "shoot_day_id", "scene_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_project_scene_num",
                table: "scenes",
                columns: new[] { "project_id", "sequence_num" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_script_elements_role_id",
                table: "script_elements",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "uq_scene_element_order",
                table: "script_elements",
                columns: new[] { "scene_id", "order_index" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_shoot_days_base_location_id",
                table: "shoot_days",
                column: "base_location_id");

            migrationBuilder.CreateIndex(
                name: "unq_project_unit_shift",
                table: "shoot_days",
                columns: new[] { "project_id", "unit_name", "shift_start" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "users_email_key",
                table: "users",
                column: "email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "call_sheets");

            migrationBuilder.DropTable(
                name: "casting");

            migrationBuilder.DropTable(
                name: "project_members");

            migrationBuilder.DropTable(
                name: "props");

            migrationBuilder.DropTable(
                name: "scene_resource");

            migrationBuilder.DropTable(
                name: "scene_schedule");

            migrationBuilder.DropTable(
                name: "script_elements");

            migrationBuilder.DropTable(
                name: "actors");

            migrationBuilder.DropTable(
                name: "shoot_days");

            migrationBuilder.DropTable(
                name: "roles");

            migrationBuilder.DropTable(
                name: "scenes");

            migrationBuilder.DropTable(
                name: "locations");

            migrationBuilder.DropTable(
                name: "projects");

            migrationBuilder.DropTable(
                name: "resources");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
