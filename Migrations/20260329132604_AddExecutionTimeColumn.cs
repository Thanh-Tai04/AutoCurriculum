using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoCurriculum.Migrations
{
    /// <inheritdoc />
    public partial class AddExecutionTimeColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "RuntimeMs",
                table: "SystemLogs",
                newName: "ExecutionTimeMs");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ExecutionTimeMs",
                table: "SystemLogs",
                newName: "RuntimeMs");
        }
    }
}
