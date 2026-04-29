using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CineCoreBack.Migrations
{
    /// <inheritdoc />
    public partial class ImplementDatabaseOptimizations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {


            migrationBuilder.Sql(@"
        CREATE OR REPLACE PROCEDURE sp_reorder_scenes(p_project_id INT, p_scene_ids INT[])
        LANGUAGE plpgsql AS $$
        DECLARE i INT;
        BEGIN
            UPDATE scenes SET sequence_num = -(sequence_num + 1000000) WHERE project_id = p_project_id;
            FOR i IN 1 .. array_length(p_scene_ids, 1) LOOP
                UPDATE scenes SET sequence_num = i WHERE id = p_scene_ids[i] AND project_id = p_project_id;
            END LOOP;
        END;
        $$;
    ");

            migrationBuilder.Sql(@"
        CREATE OR REPLACE VIEW vw_project_dashboard_stats AS
        SELECT 
            p.id AS project_id,
            (SELECT COUNT(*) FROM scenes s WHERE s.project_id = p.id) AS total_scenes,
            (SELECT COUNT(DISTINCT s.id) FROM scenes s JOIN scene_schedule ss ON s.id = ss.scene_id JOIN shoot_days sd ON ss.shoot_day_id = sd.id WHERE s.project_id = p.id AND sd.status = 'published') AS completed_scenes,
            (SELECT COUNT(*) FROM roles r WHERE r.project_id = p.id) AS total_roles,
            (SELECT COUNT(DISTINCT c.role_id) FROM casting c JOIN roles r ON c.role_id = r.id WHERE r.project_id = p.id AND c.cast_status = 'approved') AS cast_roles,
            (SELECT COUNT(*) FROM resources res JOIN locations l ON res.id = l.id WHERE res.project_id = p.id) AS total_locations,
            (SELECT COUNT(*) FROM resources res JOIN props pr ON res.id = pr.id WHERE res.project_id = p.id) AS total_props,
            (SELECT COUNT(*) FROM project_invitations pi WHERE pi.project_id = p.id) AS pending_invites
        FROM projects p;
    ");

            migrationBuilder.Sql(@"
        CREATE OR REPLACE VIEW vw_planner_scenes AS
        SELECT 
            s.id AS scene_id, s.project_id, s.sequence_num, s.slugline_text, s.estimated_duration,
            (SELECT l.location_name FROM scene_resource sr JOIN resources res ON sr.resource_id = res.id JOIN locations l ON res.id = l.id WHERE sr.scene_id = s.id LIMIT 1) AS primary_location_name,
            (SELECT STRING_AGG(DISTINCT r.role_name, ', ') FROM script_elements se JOIN roles r ON se.role_id = r.id WHERE se.scene_id = s.id AND r.role_name IS NOT NULL) AS cast_names
        FROM scenes s;
    ");

            migrationBuilder.Sql(@"
        CREATE OR REPLACE FUNCTION fn_clean_ghost_roles() RETURNS TRIGGER AS $$
        DECLARE v_is_auto BOOLEAN; v_elements_count INT;
        BEGIN
            IF OLD.role_id IS NOT NULL THEN
                SELECT is_auto_generated INTO v_is_auto FROM roles WHERE id = OLD.role_id;
                IF v_is_auto THEN
                    SELECT COUNT(*) INTO v_elements_count FROM script_elements WHERE role_id = OLD.role_id;
                    IF v_elements_count = 0 THEN
                        DELETE FROM resources WHERE id = OLD.role_id;
                    END IF;
                END IF;
            END IF;
            RETURN OLD;
        END;
        $$ LANGUAGE plpgsql;

        CREATE TRIGGER trg_ghost_buster AFTER DELETE ON script_elements FOR EACH ROW EXECUTE FUNCTION fn_clean_ghost_roles();
    ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
