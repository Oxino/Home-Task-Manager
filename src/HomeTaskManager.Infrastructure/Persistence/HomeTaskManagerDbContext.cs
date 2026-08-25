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

    public DbSet<SystemSetting> SystemSettings => Set<SystemSetting>();

    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<TaskComment> TaskComments => Set<TaskComment>();

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

        modelBuilder.Entity<SystemSetting>(entity =>
        {
            entity.HasKey(setting => setting.Id);
            entity.HasIndex(setting => setting.Key).IsUnique();
            entity.Property(setting => setting.Key).HasMaxLength(200).IsRequired();
            entity.Property(setting => setting.Value).HasMaxLength(1000).IsRequired();
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(entry => entry.Id);
            entity.HasIndex(entry => new { entry.EntityType, entry.EntityId });
            entity.HasIndex(entry => entry.PerformedAt);
            entity.Property(entry => entry.EntityType).HasMaxLength(100).IsRequired();
            entity.Property(entry => entry.Action).HasMaxLength(100).IsRequired();
        });

        modelBuilder.Entity<TaskComment>(entity =>
        {
            entity.HasKey(comment => comment.Id);
            entity.HasIndex(comment => new { comment.TaskInstanceId, comment.CreatedAt });
            entity.Property(comment => comment.Content).HasMaxLength(2000).IsRequired();
        });
    }
}