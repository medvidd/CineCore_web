using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CineCoreBack.Migrations
{
    /// <inheritdoc />
    public partial class UpdatedShootDayStatusEnum : Migration
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
                .Annotation("Npgsql:Enum:enm_shoot_day_status", "draft,generated,published,completed")
                .Annotation("Npgsql:Enum:enm_system_role", "owner,manager,actor")
                .OldAnnotation("Npgsql:Enum:enm_acquisition_type", "buy,rent")
                .OldAnnotation("Npgsql:Enum:enm_cast_status", "pending,approved,hold,declined")
                .OldAnnotation("Npgsql:Enum:enm_gender", "f,m,any")
                .OldAnnotation("Npgsql:Enum:enm_location_type", "interior,exterior,studio")
                .OldAnnotation("Npgsql:Enum:enm_member_status", "pending,active,declined")
                .OldAnnotation("Npgsql:Enum:enm_prop_status", "available,leased,unavailable")
                .OldAnnotation("Npgsql:Enum:enm_prop_type", "action,scenography,functional")
                .OldAnnotation("Npgsql:Enum:enm_resource_type", "ROLE,LOCATION,PROP")
                .OldAnnotation("Npgsql:Enum:enm_role_type", "lead,supporting,extra")
                .OldAnnotation("Npgsql:Enum:enm_scene_status", "draft,complete")
                .OldAnnotation("Npgsql:Enum:enm_script_element", "action,character,dialogue,parenthetical,transition,shot")
                .OldAnnotation("Npgsql:Enum:enm_shoot_day_status", "draft,published,completed,cancelled")
                .OldAnnotation("Npgsql:Enum:enm_system_role", "owner,manager,actor");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
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
                .Annotation("Npgsql:Enum:enm_system_role", "owner,manager,actor")
                .OldAnnotation("Npgsql:Enum:enm_acquisition_type", "buy,rent")
                .OldAnnotation("Npgsql:Enum:enm_cast_status", "pending,approved,hold,declined")
                .OldAnnotation("Npgsql:Enum:enm_gender", "f,m,any")
                .OldAnnotation("Npgsql:Enum:enm_location_type", "interior,exterior,studio")
                .OldAnnotation("Npgsql:Enum:enm_member_status", "pending,active,declined")
                .OldAnnotation("Npgsql:Enum:enm_prop_status", "available,leased,unavailable")
                .OldAnnotation("Npgsql:Enum:enm_prop_type", "action,scenography,functional")
                .OldAnnotation("Npgsql:Enum:enm_resource_type", "ROLE,LOCATION,PROP")
                .OldAnnotation("Npgsql:Enum:enm_role_type", "lead,supporting,extra")
                .OldAnnotation("Npgsql:Enum:enm_scene_status", "draft,complete")
                .OldAnnotation("Npgsql:Enum:enm_script_element", "action,character,dialogue,parenthetical,transition,shot")
                .OldAnnotation("Npgsql:Enum:enm_shoot_day_status", "draft,generated,published,completed")
                .OldAnnotation("Npgsql:Enum:enm_system_role", "owner,manager,actor");
        }
    }
}
