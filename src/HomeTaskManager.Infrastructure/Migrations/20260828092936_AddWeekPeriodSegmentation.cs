using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HomeTaskManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddWeekPeriodSegmentation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Weeks_Year_WeekNumber",
                table: "Weeks");

            migrationBuilder.AddColumn<bool>(
                name: "IgnoreHistoryForAssignment",
                table: "Weeks",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "SegmentNumber",
                table: "Weeks",
                type: "INTEGER",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateIndex(
                name: "IX_Weeks_Year_WeekNumber_SegmentNumber",
                table: "Weeks",
                columns: new[] { "Year", "WeekNumber", "SegmentNumber" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Weeks_Year_WeekNumber_SegmentNumber",
                table: "Weeks");

            migrationBuilder.DropColumn(
                name: "IgnoreHistoryForAssignment",
                table: "Weeks");

            migrationBuilder.DropColumn(
                name: "SegmentNumber",
                table: "Weeks");

            migrationBuilder.CreateIndex(
                name: "IX_Weeks_Year_WeekNumber",
                table: "Weeks",
                columns: new[] { "Year", "WeekNumber" },
                unique: true);
        }
    }
}
