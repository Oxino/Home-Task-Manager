using HomeTaskManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HomeTaskManager.Infrastructure.Persistence;

public sealed class HomeTaskManagerDbContext(DbContextOptions<HomeTaskManagerDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();

    public DbSet<TaskTemplate> TaskTemplates => Set<TaskTemplate>();

    public DbSet<Week> Weeks => Set<Week>();

    public DbSet<TaskInstance> TaskInstances => Set<TaskInstance>();

    public DbSet<AssignmentRun> AssignmentRuns => Set<AssignmentRun>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(user => user.Id);
            entity.HasIndex(user => user.DisplayName).IsUnique();
            entity.Property(user => user.DisplayName).HasMaxLength(200).IsRequired();
        });

        modelBuilder.Entity<TaskTemplate>(entity =>
        {
            entity.HasKey(task => task.Id);
            entity.HasIndex(task => task.Title).IsUnique();
            entity.Property(task => task.Title).HasMaxLength(200).IsRequired();
            entity.Property(task => task.Description).HasMaxLength(2000);
        });

        modelBuilder.Entity<Week>(entity =>
        {
            entity.HasKey(week => week.Id);
            entity.HasIndex(week => new { week.Year, week.WeekNumber }).IsUnique();
        });

        modelBuilder.Entity<TaskInstance>(entity =>
        {
            entity.HasKey(instance => instance.Id);
            entity.HasIndex(instance => new { instance.WeekId, instance.AssignedUserId });
            entity.HasIndex(instance => new
            {
                instance.TaskTemplateId,
                instance.WeekId,
                instance.PlannedDate
            }).IsUnique();
        });

        modelBuilder.Entity<AssignmentRun>(entity =>
        {
            entity.HasKey(run => run.Id);
            entity.HasIndex(run => run.WeekId);
            entity.Property(run => run.StrategyName).HasMaxLength(100).IsRequired();
        });
    }
}