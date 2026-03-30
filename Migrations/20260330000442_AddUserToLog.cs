using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoCurriculum.Migrations
{
    /// <inheritdoc />
    public partial class AddUserToLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UserEmail",
                table: "SystemLogs",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UserEmail",
                table: "SystemLogs");
        }
    }
}
