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
            // Use +1 and set is_called=false so the next insert yields max(id)+1, and avoid setting sequence to 0.
            migrationBuilder.Sql("SELECT setval(pg_get_serial_sequence('\"Notifications\"','id'), COALESCE((SELECT MAX(id) FROM \"Notifications\"),0) + 1, false);");

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
