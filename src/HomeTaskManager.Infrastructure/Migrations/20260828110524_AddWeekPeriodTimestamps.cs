using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HomeTaskManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddWeekPeriodTimestamps : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "EndsAt",
                table: "Weeks",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "StartsAt",
                table: "Weeks",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            if (ActiveProvider.Contains("Sqlite"))
            {
                migrationBuilder.Sql("UPDATE \"Weeks\" SET \"StartsAt\" = \"StartDate\" || 'T00:00:00.0000000Z', \"EndsAt\" = \"EndDate\" || 'T23:59:59.9999999Z';");
            }
            else if (ActiveProvider.Contains("Npgsql"))
            {
                migrationBuilder.Sql("UPDATE \"Weeks\" SET \"StartsAt\" = \"StartDate\"::timestamp AT TIME ZONE 'UTC', \"EndsAt\" = (\"EndDate\"::timestamp + INTERVAL '1 day' - INTERVAL '1 microsecond') AT TIME ZONE 'UTC';");
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EndsAt",
                table: "Weeks");

            migrationBuilder.DropColumn(
                name: "StartsAt",
                table: "Weeks");
        }
    }
}
