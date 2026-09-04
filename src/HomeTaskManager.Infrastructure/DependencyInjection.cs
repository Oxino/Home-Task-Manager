using HomeTaskManager.Application.Abstractions;
using HomeTaskManager.Application.Abstractions.Repositories;
using HomeTaskManager.Infrastructure.Persistence;
using HomeTaskManager.Infrastructure.Persistence.Repositories;
using HomeTaskManager.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HomeTaskManager.Infrastructure;

/// <summary>Registers the DbContext (SQLite by default, PostgreSQL when configured), every repository, and the technical services; called once from the Api's Program.cs.</summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var provider = configuration["Database:Provider"]?.ToUpperInvariant();
        var connectionString = configuration.GetConnectionString("Default")
            ?? "Data Source=hometaskmanager.db";

        services.AddDbContext<HomeTaskManagerDbContext>(options =>
        {
            // Self-hosted app defaults to a local SQLite file; PostgreSQL is opt-in via configuration for larger deployments.
            if (provider == "POSTGRESQL")
            {
                options.UseNpgsql(connectionString);
                return;
            }

            options.UseSqlite(connectionString);
        });

        services.AddScoped<IUnitOfWork, EfUnitOfWork>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ITaskTemplateRepository, TaskTemplateRepository>();
        services.AddScoped<IWeekRepository, WeekRepository>();
        services.AddScoped<ITaskInstanceRepository, TaskInstanceRepository>();
        services.AddScoped<IAssignmentRunRepository, AssignmentRunRepository>();
        services.AddScoped<ISystemSettingRepository, SystemSettingRepository>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();
        services.AddScoped<IAuditLogger, AuditLogger>();
        services.AddScoped<ITaskCommentRepository, TaskCommentRepository>();
        services.AddScoped<IUserPreferencesRepository, UserPreferencesRepository>();
        services.AddScoped<IReminderRepository, ReminderRepository>();
        services.AddScoped<IDailyFlexibleExecutionRepository, DailyFlexibleExecutionRepository>();
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<IReactionRepository, ReactionRepository>();
        services.AddScoped<IAssignmentExplanationRepository, AssignmentExplanationRepository>();

        services.AddScoped<INotificationService, NotificationService>();
        // Singleton is safe: SystemClock is stateless and just reads DateTime.UtcNow.
        services.AddSingleton<IClock, SystemClock>();

        return services;
    }
}