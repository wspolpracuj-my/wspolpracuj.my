using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace wspolpracujmy.Migrations
{
    /// <inheritdoc />
    [Migration("20260506153000_AddGroupRequestRelationsAndOptionalGroupProject")]
    public partial class AddGroupRequestRelationsAndOptionalGroupProject : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Groups_Project_project_id",
                table: "Groups");

            migrationBuilder.AlterColumn<int>(
                name: "project_id",
                table: "Groups",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<int>(
                name: "project_id",
                table: "GroupRequests",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_GroupRequests_created_by_user_id",
                table: "GroupRequests",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_GroupRequests_group_id",
                table: "GroupRequests",
                column: "group_id");

            migrationBuilder.CreateIndex(
                name: "IX_GroupRequests_project_id",
                table: "GroupRequests",
                column: "project_id");

            migrationBuilder.CreateIndex(
                name: "IX_GroupRequests_student_id",
                table: "GroupRequests",
                column: "student_id");

            migrationBuilder.AddForeignKey(
                name: "FK_Groups_Project_project_id",
                table: "Groups",
                column: "project_id",
                principalTable: "Project",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_GroupRequests_Group_group_id",
                table: "GroupRequests",
                column: "group_id",
                principalTable: "Groups",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_GroupRequests_Project_project_id",
                table: "GroupRequests",
                column: "project_id",
                principalTable: "Project",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_GroupRequests_Students_student_id",
                table: "GroupRequests",
                column: "student_id",
                principalTable: "Students",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_GroupRequests_Users_created_by_user_id",
                table: "GroupRequests",
                column: "created_by_user_id",
                principalTable: "Users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Groups_Project_project_id",
                table: "Groups");

            migrationBuilder.DropForeignKey(
                name: "FK_GroupRequests_Group_group_id",
                table: "GroupRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_GroupRequests_Project_project_id",
                table: "GroupRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_GroupRequests_Students_student_id",
                table: "GroupRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_GroupRequests_Users_created_by_user_id",
                table: "GroupRequests");

            migrationBuilder.DropIndex(
                name: "IX_GroupRequests_created_by_user_id",
                table: "GroupRequests");

            migrationBuilder.DropIndex(
                name: "IX_GroupRequests_group_id",
                table: "GroupRequests");

            migrationBuilder.DropIndex(
                name: "IX_GroupRequests_project_id",
                table: "GroupRequests");

            migrationBuilder.DropIndex(
                name: "IX_GroupRequests_student_id",
                table: "GroupRequests");

            migrationBuilder.DropColumn(
                name: "project_id",
                table: "GroupRequests");

            migrationBuilder.AlterColumn<int>(
                name: "project_id",
                table: "Groups",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Groups_Project_project_id",
                table: "Groups",
                column: "project_id",
                principalTable: "Project",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}