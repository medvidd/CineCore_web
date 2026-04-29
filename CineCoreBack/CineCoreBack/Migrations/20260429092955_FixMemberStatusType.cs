using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CineCoreBack.Migrations
{
    public partial class FixMemberStatusType : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Тимчасово видаляємо View, яке залежить від цієї колонки
            migrationBuilder.Sql("DROP VIEW IF EXISTS vw_project_dashboard_stats;");

            // 2. Виконуємо зміну типу колонки
            migrationBuilder.Sql("ALTER TABLE project_members ALTER COLUMN member_status DROP DEFAULT;");
            migrationBuilder.Sql("ALTER TABLE project_members ALTER COLUMN member_status TYPE character varying(50) USING member_status::text;");
            migrationBuilder.Sql("ALTER TABLE project_members ALTER COLUMN member_status SET DEFAULT 'pending';");

            // 3. Відновлюємо View
            migrationBuilder.Sql(@"
                CREATE OR REPLACE VIEW vw_project_dashboard_stats AS
                SELECT 
                    p.id AS project_id,
                    (SELECT COUNT(*) FROM scenes WHERE project_id = p.id) AS total_scenes,
                    (SELECT COUNT(DISTINCT ss.scene_id) FROM scene_schedule ss JOIN shoot_days sd ON ss.shoot_day_id = sd.id WHERE sd.project_id = p.id AND sd.status = 'completed') AS completed_scenes,
                    (SELECT COUNT(*) FROM roles WHERE project_id = p.id) AS total_roles,
                    (SELECT COUNT(DISTINCT role_id) FROM casting c JOIN roles r ON c.role_id = r.id WHERE r.project_id = p.id AND c.cast_status = 'approved') AS cast_roles,
                    (SELECT COUNT(*) FROM resources res JOIN locations l ON res.id = l.id WHERE res.project_id = p.id) AS total_locations,
                    (SELECT COUNT(*) FROM resources res JOIN props pr ON res.id = pr.id WHERE res.project_id = p.id) AS total_props,
                    (
                        (SELECT COUNT(*) FROM project_invitations WHERE project_id = p.id) + 
                        (SELECT COUNT(*) FROM project_members WHERE project_id = p.id AND member_status = 'pending')
                    ) AS pending_invites
                FROM projects p;
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP VIEW IF EXISTS vw_project_dashboard_stats;");

            migrationBuilder.Sql("ALTER TABLE project_members ALTER COLUMN member_status DROP DEFAULT;");
            migrationBuilder.Sql("ALTER TABLE project_members ALTER COLUMN member_status TYPE enm_member_status USING member_status::enm_member_status;");
            migrationBuilder.Sql("ALTER TABLE project_members ALTER COLUMN member_status SET DEFAULT 'pending'::enm_member_status;");

            migrationBuilder.Sql(@"
                CREATE OR REPLACE VIEW vw_project_dashboard_stats AS
                SELECT 
                    p.id AS project_id,
                    (SELECT COUNT(*) FROM scenes WHERE project_id = p.id) AS total_scenes,
                    (SELECT COUNT(DISTINCT ss.scene_id) FROM scene_schedule ss JOIN shoot_days sd ON ss.shoot_day_id = sd.id WHERE sd.project_id = p.id AND sd.status = 'completed') AS completed_scenes,
                    (SELECT COUNT(*) FROM roles WHERE project_id = p.id) AS total_roles,
                    (SELECT COUNT(DISTINCT role_id) FROM casting c JOIN roles r ON c.role_id = r.id WHERE r.project_id = p.id AND c.cast_status = 'approved') AS cast_roles,
                    (SELECT COUNT(*) FROM resources res JOIN locations l ON res.id = l.id WHERE res.project_id = p.id) AS total_locations,
                    (SELECT COUNT(*) FROM resources res JOIN props pr ON res.id = pr.id WHERE res.project_id = p.id) AS total_props,
                    (
                        (SELECT COUNT(*) FROM project_invitations WHERE project_id = p.id) + 
                        (SELECT COUNT(*) FROM project_members WHERE project_id = p.id AND member_status = 'pending')
                    ) AS pending_invites
                FROM projects p;
            ");
        }
    }
}