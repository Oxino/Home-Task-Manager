using HomeTaskManager.Domain.Entities;

namespace HomeTaskManager.Application.UseCases.Weeks;

internal static class WeekMapper
{
    public static WeekDto ToDto(Week week) =>
        new(week.Id, week.Year, week.WeekNumber, week.StartDate, week.EndDate, week.Status.ToString());

    public static TaskInstanceDto ToDto(TaskInstance instance) => new(
        instance.Id,
        instance.TaskTemplateId,
        instance.WeekId,
        instance.PlannedDate,
        instance.DueDate,
        instance.Status.ToString(),
        instance.IsCompleted,
        instance.CompletedAt,
        instance.IsCarryOver,
        instance.AssignedUserId,
        instance.ParentTaskInstanceId);
}
