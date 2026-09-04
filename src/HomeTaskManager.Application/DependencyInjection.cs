using HomeTaskManager.Application.UseCases.Analytics;
using HomeTaskManager.Application.UseCases.Assignments;
using HomeTaskManager.Application.UseCases.Audit;
using HomeTaskManager.Application.UseCases.Settings;
using HomeTaskManager.Application.UseCases.TaskInstances;
using HomeTaskManager.Application.UseCases.TaskTemplates;
using HomeTaskManager.Application.UseCases.Users;
using HomeTaskManager.Application.UseCases.Weeks;
using HomeTaskManager.Application.UseCases.Notifications;
using HomeTaskManager.Domain.Engine;
using Microsoft.Extensions.DependencyInjection;

namespace HomeTaskManager.Application;

/// <summary>Registers every use case (and the pluggable assignment strategies) for DI; called once from the Api's Program.cs.</summary>
public static class DependencyInjection
{
    /// <summary>Adds all Application-layer services as Scoped (one instance per HTTP request) to the DI container.</summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // --- Users / profiles ---
        services.AddScoped<ICreateUserUseCase, CreateUserUseCase>();
        services.AddScoped<IListActiveProfilesUseCase, ListActiveProfilesUseCase>();
        services.AddScoped<ISelectProfileUseCase, SelectProfileUseCase>();
        services.AddScoped<IUpdateUserUseCase, UpdateUserUseCase>();

        // --- Task templates / comments / reminders / preferences ---
        services.AddScoped<ICreateTaskTemplateUseCase, CreateTaskTemplateUseCase>();
        services.AddScoped<IListActiveTaskTemplatesUseCase, ListActiveTaskTemplatesUseCase>();
        services.AddScoped<IUpdateTaskTemplateUseCase, UpdateTaskTemplateUseCase>();
        services.AddScoped<ISetTaskTemplateActiveUseCase, SetTaskTemplateActiveUseCase>();
        services.AddScoped<ITaskCommentUseCase, TaskCommentUseCase>();
        services.AddScoped<IUserPreferencesUseCase, UserPreferencesUseCase>();
        services.AddScoped<IReminderUseCase, ReminderUseCase>();
        services.AddScoped<IExecuteDailyFlexibleTaskUseCase, ExecuteDailyFlexibleTaskUseCase>();
        services.AddScoped<INotificationUseCase, NotificationUseCase>();
        services.AddScoped<IProcessReminderNotificationsUseCase, ProcessReminderNotificationsUseCase>();

        // --- Weeks / task instances lifecycle ---
        services.AddScoped<IGenerateWeekInstancesUseCase, GenerateWeekInstancesUseCase>();
        services.AddScoped<IGetWeekInstancesUseCase, GetWeekInstancesUseCase>();
        services.AddScoped<ICloseWeekUseCase, CloseWeekUseCase>();
        services.AddScoped<IRecoverWeekContinuationUseCase, RecoverWeekContinuationUseCase>();
        services.AddScoped<ISetTaskInstanceCompletionUseCase, SetTaskInstanceCompletionUseCase>();
        services.AddScoped<IReassignTaskInstanceUseCase, ReassignTaskInstanceUseCase>();
        services.AddScoped<ICancelTaskInstanceUseCase, CancelTaskInstanceUseCase>();
        services.AddScoped<IAddReactionUseCase, AddReactionUseCase>();
        services.AddScoped<IRemoveReactionUseCase, RemoveReactionUseCase>();
        services.AddScoped<IListReactionsForCommentUseCase, ListReactionsForCommentUseCase>();

        // --- Assignment engine: every IAssignmentStrategy implementation is registered so RunAssignmentEngineUseCase
        // can resolve the one matching the admin-configured StrategyName (ADR-002, keep-open/closed for new strategies). ---
        services.AddScoped<IAssignmentStrategy, IntelligentV1Strategy>();
        services.AddScoped<IAssignmentStrategy, RandomStrategy>();
        services.AddScoped<IAssignmentStrategy, BalancedPointsStrategy>();
        services.AddScoped<IAssignmentStrategy, BalancedTimeStrategy>();
        services.AddScoped<IAssignmentEngineSettingsProvider, AssignmentEngineSettingsProvider>();
        services.AddScoped<IGetAssignmentEngineSettingsUseCase, GetAssignmentEngineSettingsUseCase>();
        services.AddScoped<IUpdateAssignmentEngineSettingsUseCase, UpdateAssignmentEngineSettingsUseCase>();
        services.AddScoped<IRunAssignmentEngineUseCase, RunAssignmentEngineUseCase>();

        // --- Audit / analytics (read-only reporting use cases) ---
        services.AddScoped<IGetAuditLogsForEntityUseCase, GetAuditLogsForEntityUseCase>();

        services.AddScoped<IGetWeekStatisticsUseCase, GetWeekStatisticsUseCase>();
        services.AddScoped<IGetMonthStatisticsUseCase, GetMonthStatisticsUseCase>();
        services.AddScoped<IGetCompletionStatisticsUseCase, GetCompletionStatisticsUseCase>();
        services.AddScoped<IGetUserLoadDistributionUseCase, GetUserLoadDistributionUseCase>();

        return services;
    }
}
