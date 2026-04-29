using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace wspolpracujmy.Migrations
{
    /// <inheritdoc />
    public partial class FixNotificationIdentity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Ensure the sequence for Notifications.id is advanced to at least the current max id
            migrationBuilder.Sql("SELECT setval(pg_get_serial_sequence('\"Notifications\"','id'), (SELECT COALESCE(MAX(id),0) FROM \"Notifications\"));");

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
