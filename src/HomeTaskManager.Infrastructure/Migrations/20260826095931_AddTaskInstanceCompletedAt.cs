using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HomeTaskManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTaskInstanceCompletedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CompletedAt",
                table: "TaskInstances",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CompletedAt",
                table: "TaskInstances");
        }
    }
}
