using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace wspolpracujmy.Migrations
{
    /// <inheritdoc />
    public partial class AuthFrontendBackend : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "responded_by_user_id",
                table: "GroupRequests",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_GroupRequests_responded_by_user_id",
                table: "GroupRequests",
                column: "responded_by_user_id");

            migrationBuilder.AddForeignKey(
                name: "FK_GroupRequests_Users_responded_by_user_id",
                table: "GroupRequests",
                column: "responded_by_user_id",
                principalTable: "Users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GroupRequests_Users_responded_by_user_id",
                table: "GroupRequests");

            migrationBuilder.DropIndex(
                name: "IX_GroupRequests_responded_by_user_id",
                table: "GroupRequests");

            migrationBuilder.DropColumn(
                name: "responded_by_user_id",
                table: "GroupRequests");
        }
    }
}
