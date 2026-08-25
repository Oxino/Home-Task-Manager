using HomeTaskManager.Application.Abstractions;
using HomeTaskManager.Application.Abstractions.Repositories;
using HomeTaskManager.Infrastructure.Persistence;
using HomeTaskManager.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HomeTaskManager.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var provider = configuration["Database:Provider"]?.ToUpperInvariant();
        var connectionString = configuration.GetConnectionString("Default")
            ?? "Data Source=hometaskmanager.db";

        services.AddDbContext<HomeTaskManagerDbContext>(options =>
        {
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

        return services;
    }
}