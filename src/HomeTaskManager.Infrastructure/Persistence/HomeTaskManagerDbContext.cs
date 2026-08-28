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
    public DbSet<UserPreferences> UserPreferences => Set<UserPreferences>();
    public DbSet<Reminder> Reminders => Set<Reminder>();
    public DbSet<DailyFlexibleExecution> DailyFlexibleExecutions => Set<DailyFlexibleExecution>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<Reaction> Reactions => Set<Reaction>();

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
            entity.Property(task => task.ActiveWeekDays).HasMaxLength(100).IsRequired();
            entity.Property(task => task.MonthlyScheduleType).IsRequired();
            entity.Property(task => task.WeeklyScheduleType).IsRequired();
        });

        modelBuilder.Entity<Week>(entity =>
        {
            entity.HasKey(week => week.Id);
            entity.HasIndex(week => new { week.Year, week.WeekNumber, week.SegmentNumber }).IsUnique();
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
            entity.HasIndex(comment => comment.ParentCommentId);
            entity.Property(comment => comment.Content).HasMaxLength(2000).IsRequired();
            entity.Property(comment => comment.ParentCommentId);
        });

        modelBuilder.Entity<UserPreferences>(entity =>
        {
            entity.HasKey(preferences => preferences.Id);
            entity.HasIndex(preferences => preferences.UserId).IsUnique();
            entity.Property(preferences => preferences.DateDisplayFormat).HasMaxLength(20).IsRequired();
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.Property(notification => notification.Importance).HasConversion<int>().IsRequired();
        });

        modelBuilder.Entity<Reminder>(entity =>
        {
            entity.HasKey(reminder => reminder.Id);
            entity.HasIndex(reminder => reminder.TaskTemplateId);
            entity.Property(reminder => reminder.CreatedAt).IsRequired();
            entity.Property(reminder => reminder.UpdatedAt).IsRequired();
        });

        modelBuilder.Entity<DailyFlexibleExecution>(entity =>
        {
            entity.HasKey(execution => execution.Id);
            entity.HasIndex(execution => new { execution.TaskInstanceId, execution.ExecutionDate });
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(notification => notification.Id);
            entity.HasIndex(notification => new { notification.UserId, notification.CreatedAt });
            entity.Property(notification => notification.Title).HasMaxLength(200).IsRequired();
            entity.Property(notification => notification.Message).HasMaxLength(2000).IsRequired();
            entity.Property(notification => notification.Link).HasMaxLength(500);
        });

        modelBuilder.Entity<Reaction>(entity =>
        {
            entity.HasKey(reaction => reaction.Id);
            entity.HasIndex(reaction => reaction.CommentId);
            entity.HasIndex(reaction => new { reaction.CommentId, reaction.UserId, reaction.ReactionType }).IsUnique();
            entity.Property(reaction => reaction.ReactionType).HasMaxLength(50).IsRequired();
        });
    }
}
