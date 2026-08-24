using HomeTaskManager.Domain.Entities;

namespace HomeTaskManager.Application.UseCases.TaskTemplates;

internal static class TaskTemplateMapper
{
    public static TaskTemplateDto ToDto(TaskTemplate taskTemplate) => new(
        taskTemplate.Id,
        taskTemplate.Title,
        taskTemplate.Description,
        taskTemplate.ManualDifficultyScore,
        taskTemplate.Priority.ToString(),
        taskTemplate.EstimatedDurationMinutes,
        taskTemplate.FrequencyType.ToString(),
        taskTemplate.IsActive,
        taskTemplate.CreatedByUserId);
}
