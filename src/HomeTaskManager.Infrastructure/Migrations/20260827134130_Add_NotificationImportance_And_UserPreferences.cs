using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HomeTaskManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Add_NotificationImportance_And_UserPreferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NotificationsEnabled",
                table: "UserPreferences");

            migrationBuilder.AddColumn<string>(
                name: "NotificationPreference",
                table: "UserPreferences",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Importance",
                table: "Notifications",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NotificationPreference",
                table: "UserPreferences");

            migrationBuilder.DropColumn(
                name: "Importance",
                table: "Notifications");

            migrationBuilder.AddColumn<bool>(
                name: "NotificationsEnabled",
                table: "UserPreferences",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }
    }
}
