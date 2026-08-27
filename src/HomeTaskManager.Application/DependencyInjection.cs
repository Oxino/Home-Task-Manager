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

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ICreateUserUseCase, CreateUserUseCase>();
        services.AddScoped<IListActiveProfilesUseCase, ListActiveProfilesUseCase>();
        services.AddScoped<ISelectProfileUseCase, SelectProfileUseCase>();
        services.AddScoped<IUpdateUserUseCase, UpdateUserUseCase>();

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

        services.AddScoped<IGenerateWeekInstancesUseCase, GenerateWeekInstancesUseCase>();
        services.AddScoped<IGetWeekInstancesUseCase, GetWeekInstancesUseCase>();
        services.AddScoped<ICloseWeekUseCase, CloseWeekUseCase>();
        services.AddScoped<ISetTaskInstanceCompletionUseCase, SetTaskInstanceCompletionUseCase>();
        services.AddScoped<IReassignTaskInstanceUseCase, ReassignTaskInstanceUseCase>();
        services.AddScoped<ICancelTaskInstanceUseCase, CancelTaskInstanceUseCase>();
        services.AddScoped<IAddReactionUseCase, AddReactionUseCase>();
        services.AddScoped<IRemoveReactionUseCase, RemoveReactionUseCase>();
        services.AddScoped<IListReactionsForCommentUseCase, ListReactionsForCommentUseCase>();

        services.AddScoped<IAssignmentStrategy, IntelligentV1Strategy>();
        services.AddScoped<IAssignmentStrategy, RandomStrategy>();
        services.AddScoped<IAssignmentStrategy, BalancedPointsStrategy>();
        services.AddScoped<IAssignmentStrategy, BalancedTimeStrategy>();
        services.AddScoped<IAssignmentEngineSettingsProvider, AssignmentEngineSettingsProvider>();
        services.AddScoped<IGetAssignmentEngineSettingsUseCase, GetAssignmentEngineSettingsUseCase>();
        services.AddScoped<IUpdateAssignmentEngineSettingsUseCase, UpdateAssignmentEngineSettingsUseCase>();
        services.AddScoped<IRunAssignmentEngineUseCase, RunAssignmentEngineUseCase>();

        services.AddScoped<IGetAuditLogsForEntityUseCase, GetAuditLogsForEntityUseCase>();

        services.AddScoped<IGetWeekStatisticsUseCase, GetWeekStatisticsUseCase>();
        services.AddScoped<IGetCompletionStatisticsUseCase, GetCompletionStatisticsUseCase>();
        services.AddScoped<IGetUserLoadDistributionUseCase, GetUserLoadDistributionUseCase>();

        return services;
    }
}
