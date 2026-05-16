using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace wspolpracujmy.Migrations
{
    /// <inheritdoc />
    public partial class PatchMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE lower(table_name) = 'grouprequests' AND lower(column_name) = 'responded_by_user_id'
    ) THEN
        ALTER TABLE ""GroupRequests"" ADD COLUMN responded_by_user_id integer;
    END IF;

    IF NOT EXISTS (
        SELECT 1 FROM pg_indexes
        WHERE lower(tablename) = 'grouprequests' AND lower(indexname) = 'ix_grouprequests_responded_by_user_id'
    ) THEN
        CREATE INDEX ""IX_GroupRequests_responded_by_user_id"" ON ""GroupRequests"" (responded_by_user_id);
    END IF;

    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint WHERE conname = 'FK_GroupRequests_Users_responded_by_user_id'
    ) THEN
        ALTER TABLE ""GroupRequests""
        ADD CONSTRAINT ""FK_GroupRequests_Users_responded_by_user_id""
        FOREIGN KEY (responded_by_user_id) REFERENCES ""Users"" (id) ON DELETE RESTRICT;
    END IF;
END
$$;
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DO $$
BEGIN
    IF EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE lower(table_name) = 'grouprequests' AND lower(column_name) = 'responded_by_user_id'
    ) THEN
        IF EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_GroupRequests_Users_responded_by_user_id') THEN
            ALTER TABLE ""GroupRequests"" DROP CONSTRAINT ""FK_GroupRequests_Users_responded_by_user_id"";
        END IF;

        IF EXISTS (SELECT 1 FROM pg_indexes WHERE lower(tablename) = 'grouprequests' AND lower(indexname) = 'ix_grouprequests_responded_by_user_id') THEN
            DROP INDEX ""IX_GroupRequests_responded_by_user_id"";
        END IF;

        ALTER TABLE ""GroupRequests"" DROP COLUMN IF EXISTS responded_by_user_id;
    END IF;
END
$$;
");
        }
    }
}
