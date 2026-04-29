using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CineCoreBack.Migrations
{
    /// <inheritdoc />
    public partial class DeferGhostBusterTrigger : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
        DROP TRIGGER IF EXISTS trg_ghost_buster ON script_elements;

        CREATE CONSTRAINT TRIGGER trg_ghost_buster
        AFTER DELETE ON script_elements
        DEFERRABLE INITIALLY DEFERRED
        FOR EACH ROW EXECUTE FUNCTION fn_clean_ghost_roles();
    ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
        DROP TRIGGER IF EXISTS trg_ghost_buster ON script_elements;

        CREATE TRIGGER trg_ghost_buster
        AFTER DELETE ON script_elements
        FOR EACH ROW EXECUTE FUNCTION fn_clean_ghost_roles();
    ");
        }
    }
}
