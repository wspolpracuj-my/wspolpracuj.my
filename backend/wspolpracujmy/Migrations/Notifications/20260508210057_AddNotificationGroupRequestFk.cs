using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace wspolpracujmy.Migrations.Notifications
{
    /// <inheritdoc />
    public partial class AddNotificationGroupRequestFk : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "group_request_id",
                table: "Notifications",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_group_request_id",
                table: "Notifications",
                column: "group_request_id");

            migrationBuilder.AddForeignKey(
                name: "FK_Notifications_GroupRequests_group_request_id",
                table: "Notifications",
                column: "group_request_id",
                principalTable: "GroupRequests",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Notifications_GroupRequests_group_request_id",
                table: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_group_request_id",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "group_request_id",
                table: "Notifications");
        }
    }
}
