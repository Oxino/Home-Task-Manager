using HomeTaskManager.Application.UseCases.Assignments;
using HomeTaskManager.Application.UseCases.Audit;
using HomeTaskManager.Application.UseCases.Settings;
using HomeTaskManager.Application.UseCases.TaskInstances;
using HomeTaskManager.Application.UseCases.TaskTemplates;
using HomeTaskManager.Application.UseCases.Users;
using HomeTaskManager.Application.UseCases.Weeks;
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

        services.AddScoped<ICreateTaskTemplateUseCase, CreateTaskTemplateUseCase>();
        services.AddScoped<IListActiveTaskTemplatesUseCase, ListActiveTaskTemplatesUseCase>();
        services.AddScoped<IUpdateTaskTemplateUseCase, UpdateTaskTemplateUseCase>();

        services.AddScoped<IGenerateWeekInstancesUseCase, GenerateWeekInstancesUseCase>();
        services.AddScoped<IGetWeekInstancesUseCase, GetWeekInstancesUseCase>();
        services.AddScoped<ICloseWeekUseCase, CloseWeekUseCase>();
        services.AddScoped<ISetTaskInstanceCompletionUseCase, SetTaskInstanceCompletionUseCase>();

        // V1 default strategy per PRODUCT_VISION; RandomStrategy remains available as an alternate registration.
        services.AddScoped<IAssignmentStrategy, IntelligentV1Strategy>();
        services.AddScoped<IAssignmentEngineSettingsProvider, AssignmentEngineSettingsProvider>();
        services.AddScoped<IGetAssignmentEngineSettingsUseCase, GetAssignmentEngineSettingsUseCase>();
        services.AddScoped<IUpdateAssignmentEngineSettingsUseCase, UpdateAssignmentEngineSettingsUseCase>();
        services.AddScoped<IRunAssignmentEngineUseCase, RunAssignmentEngineUseCase>();

        services.AddScoped<IGetAuditLogsForEntityUseCase, GetAuditLogsForEntityUseCase>();

        return services;
    }
}
