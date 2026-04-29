using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace wspolpracujmy.Migrations
{
    /// <inheritdoc />
    public partial class DropNumberOfMembers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "number_of_members",
                table: "Groups");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "number_of_members",
                table: "Groups",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
