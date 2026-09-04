using HomeTaskManager.Domain.Entities;

namespace HomeTaskManager.Application.UseCases.TaskTemplates;

public static class TaskTemplateMapper
{
    /// <summary>Flat projection; GetActiveWeekDays() re-parses the entity's comma-separated storage column into a list.</summary>
    public static TaskTemplateDto ToDto(TaskTemplate taskTemplate) => new(
        taskTemplate.Id,
        taskTemplate.Title,
        taskTemplate.Description,
        taskTemplate.ManualDifficultyScore,
        taskTemplate.Priority.ToString(),
        taskTemplate.EstimatedDurationMinutes,
        taskTemplate.FrequencyType.ToString(),
        taskTemplate.IsActive,
        taskTemplate.CreatedByUserId,
        taskTemplate.GetActiveWeekDays(),
        taskTemplate.MonthlyScheduleType,
        taskTemplate.MonthlyDay,
        taskTemplate.WeeklyScheduleType,
        taskTemplate.DistributeDailyMandatory);
}
