using HomeTaskManager.Domain.Entities;

namespace HomeTaskManager.Application.UseCases.Weeks;

public static class WeekMapper
{
    /// <summary>Flat 1:1 projection; enums are serialized as strings for the frontend.</summary>
    public static WeekDto ToDto(Week week) =>
        new(week.Id, week.Year, week.WeekNumber, week.StartDate, week.EndDate, week.Status.ToString(), week.SegmentNumber, week.IgnoreHistoryForAssignment, week.StartsAt, week.EndsAt, week.ClosedAt);

    /// <summary>Original* carry-over fields are left null here; <see cref="HomeTaskManager.Application.UseCases.Weeks.GetWeekInstancesUseCase"/> fills them in when needed.</summary>
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
