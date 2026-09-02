using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HomeTaskManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAssignmentExplanation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AssignmentExplanations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TaskInstanceId = table.Column<Guid>(type: "TEXT", nullable: false),
                    AssignmentRunId = table.Column<Guid>(type: "TEXT", nullable: false),
                    AssignedUserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    EngineName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    SummaryText = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    StepsJson = table.Column<string>(type: "TEXT", nullable: false),
                    GeneratedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssignmentExplanations", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AssignmentExplanations_TaskInstanceId",
                table: "AssignmentExplanations",
                column: "TaskInstanceId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AssignmentExplanations");
        }
    }
}
