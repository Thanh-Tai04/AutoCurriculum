using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoCurriculum.Migrations
{
    /// <inheritdoc />
    public partial class DropOldUserTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CurriculumHistory_User_CreatedByNavigationUserId",
                table: "CurriculumHistory");

            migrationBuilder.DropForeignKey(
                name: "FK_Topics_User_CreatedByNavigationUserId",
                table: "Topics");

            migrationBuilder.DropTable(
                name: "User");

            migrationBuilder.DropIndex(
                name: "IX_Topics_CreatedByNavigationUserId",
                table: "Topics");

            migrationBuilder.DropIndex(
                name: "IX_CurriculumHistory_CreatedByNavigationUserId",
                table: "CurriculumHistory");

            migrationBuilder.DropColumn(
                name: "CreatedByNavigationUserId",
                table: "Topics");

            migrationBuilder.DropColumn(
                name: "CreatedByNavigationUserId",
                table: "CurriculumHistory");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CreatedByNavigationUserId",
                table: "Topics",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CreatedByNavigationUserId",
                table: "CurriculumHistory",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "User",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Username = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_User", x => x.UserId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Topics_CreatedByNavigationUserId",
                table: "Topics",
                column: "CreatedByNavigationUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CurriculumHistory_CreatedByNavigationUserId",
                table: "CurriculumHistory",
                column: "CreatedByNavigationUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_CurriculumHistory_User_CreatedByNavigationUserId",
                table: "CurriculumHistory",
                column: "CreatedByNavigationUserId",
                principalTable: "User",
                principalColumn: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Topics_User_CreatedByNavigationUserId",
                table: "Topics",
                column: "CreatedByNavigationUserId",
                principalTable: "User",
                principalColumn: "UserId");
        }
    }
}
