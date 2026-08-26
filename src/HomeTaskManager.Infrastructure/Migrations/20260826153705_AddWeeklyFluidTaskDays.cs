using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HomeTaskManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddWeeklyFluidTaskDays : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ActiveWeekDays",
                table: "TaskTemplates",
                type: "TEXT",
                maxLength: 100,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ActiveWeekDays",
                table: "TaskTemplates");
        }
    }
}
