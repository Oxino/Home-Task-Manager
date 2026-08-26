using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HomeTaskManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMonthlySchedule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MonthlyDay",
                table: "TaskTemplates",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MonthlyScheduleType",
                table: "TaskTemplates",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MonthlyDay",
                table: "TaskTemplates");

            migrationBuilder.DropColumn(
                name: "MonthlyScheduleType",
                table: "TaskTemplates");
        }
    }
}
