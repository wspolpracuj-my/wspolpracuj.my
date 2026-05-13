using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace wspolpracujmy.Migrations
{
    /// <inheritdoc />
    public partial class AddGroupRequestUniqueIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("CREATE UNIQUE INDEX IF NOT EXISTS ux_group_invite_pending\n  ON \"GroupRequests\"(group_id, student_id)\n  WHERE type = 'Invitation' AND status = 'Pending';");

            migrationBuilder.Sql("CREATE UNIQUE INDEX IF NOT EXISTS ux_group_application_pending\n  ON \"GroupRequests\"(group_id, student_id)\n  WHERE type = 'Application' AND status = 'Pending';");

                                        migrationBuilder.Sql(@"DO $$
                BEGIN
                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'GroupRequests' AND column_name = 'project_id') THEN
                        CREATE UNIQUE INDEX IF NOT EXISTS ux_group_projectreq_pending
                            ON ""GroupRequests""(group_id, project_id)
                            WHERE type = 'ProjectRequest' AND status = 'Pending' AND project_id IS NOT NULL;
                    END IF;
                END$$;");

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX IF EXISTS ux_group_invite_pending;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS ux_group_application_pending;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS ux_group_projectreq_pending;");

        }
    }
}
