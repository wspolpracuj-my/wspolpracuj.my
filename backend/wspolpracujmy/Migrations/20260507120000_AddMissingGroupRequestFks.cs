using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace wspolpracujmy.Migrations
{
    [DbContext(typeof(wspolpracujmy.Data.AppDbContext))]
    [Migration("20260507120000_AddMissingGroupRequestFks")]
    public partial class AddMissingGroupRequestFks : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add project_id column if missing
            migrationBuilder.Sql(@"ALTER TABLE ""GroupRequests"" ADD COLUMN IF NOT EXISTS project_id integer;");

            // Make Groups.project_id nullable (safe to run even if already nullable)
            migrationBuilder.Sql(@"ALTER TABLE ""Groups"" ALTER COLUMN project_id DROP NOT NULL;");

            // Create indexes if missing
            migrationBuilder.Sql(@"
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_class WHERE relname = 'IX_GroupRequests_created_by_user_id') THEN
        CREATE INDEX ""IX_GroupRequests_created_by_user_id"" ON ""GroupRequests"" (""created_by_user_id"");
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_class WHERE relname = 'IX_GroupRequests_group_id') THEN
        CREATE INDEX ""IX_GroupRequests_group_id"" ON ""GroupRequests"" (""group_id"");
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_class WHERE relname = 'IX_GroupRequests_project_id') THEN
        CREATE INDEX ""IX_GroupRequests_project_id"" ON ""GroupRequests"" (""project_id"");
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_class WHERE relname = 'IX_GroupRequests_student_id') THEN
        CREATE INDEX ""IX_GroupRequests_student_id"" ON ""GroupRequests"" (""student_id"");
    END IF;
END
$$;
");

            // Add foreign keys if missing
            migrationBuilder.Sql(@"
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_GroupRequests_Group_group_id') THEN
        ALTER TABLE ""GroupRequests"" ADD CONSTRAINT ""FK_GroupRequests_Group_group_id""
            FOREIGN KEY (""group_id"") REFERENCES ""Groups"" (""id"") ON DELETE CASCADE NOT VALID;
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_GroupRequests_Project_project_id') THEN
        ALTER TABLE ""GroupRequests"" ADD CONSTRAINT ""FK_GroupRequests_Project_project_id""
            FOREIGN KEY (""project_id"") REFERENCES ""Project"" (""id"") ON DELETE SET NULL NOT VALID;
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_GroupRequests_Students_student_id') THEN
        ALTER TABLE ""GroupRequests"" ADD CONSTRAINT ""FK_GroupRequests_Students_student_id""
            FOREIGN KEY (""student_id"") REFERENCES ""Students"" (""id"") ON DELETE CASCADE NOT VALID;
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_GroupRequests_Users_created_by_user_id') THEN
        ALTER TABLE ""GroupRequests"" ADD CONSTRAINT ""FK_GroupRequests_Users_created_by_user_id""
            FOREIGN KEY (""created_by_user_id"") REFERENCES ""Users"" (""id"") ON DELETE CASCADE NOT VALID;
    END IF;
END
$$;
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove constraints if they exist (best-effort)
            migrationBuilder.Sql(@"
DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_GroupRequests_Group_group_id') THEN
        ALTER TABLE ""GroupRequests"" DROP CONSTRAINT ""FK_GroupRequests_Group_group_id"";
    END IF;
    IF EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_GroupRequests_Project_project_id') THEN
        ALTER TABLE ""GroupRequests"" DROP CONSTRAINT ""FK_GroupRequests_Project_project_id"";
    END IF;
    IF EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_GroupRequests_Students_student_id') THEN
        ALTER TABLE ""GroupRequests"" DROP CONSTRAINT ""FK_GroupRequests_Students_student_id"";
    END IF;
    IF EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_GroupRequests_Users_created_by_user_id') THEN
        ALTER TABLE ""GroupRequests"" DROP CONSTRAINT ""FK_GroupRequests_Users_created_by_user_id"";
    END IF;
    IF EXISTS (SELECT 1 FROM pg_class WHERE relname = 'IX_GroupRequests_created_by_user_id') THEN
        DROP INDEX ""IX_GroupRequests_created_by_user_id"";
    END IF;
    IF EXISTS (SELECT 1 FROM pg_class WHERE relname = 'IX_GroupRequests_group_id') THEN
        DROP INDEX ""IX_GroupRequests_group_id"";
    END IF;
    IF EXISTS (SELECT 1 FROM pg_class WHERE relname = 'IX_GroupRequests_project_id') THEN
        DROP INDEX ""IX_GroupRequests_project_id"";
    END IF;
    IF EXISTS (SELECT 1 FROM pg_class WHERE relname = 'IX_GroupRequests_student_id') THEN
        DROP INDEX ""IX_GroupRequests_student_id"";
    END IF;
END
$$;
");
        }
    }
}
