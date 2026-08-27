using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using HomeTaskManager.Infrastructure.Persistence;

#nullable disable

namespace HomeTaskManager.Infrastructure.Migrations;

[DbContext(typeof(HomeTaskManagerDbContext))]
[Migration("20260827100000_BackfillCompletedAt")]
public partial class BackfillCompletedAt : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("UPDATE TaskInstances SET CompletedAt = UpdatedAt WHERE IsCompleted = 1 AND CompletedAt IS NULL;");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("UPDATE TaskInstances SET CompletedAt = NULL WHERE IsCompleted = 1;");
    }
}
