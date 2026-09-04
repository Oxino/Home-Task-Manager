namespace HomeTaskManager.Application.UseCases.Weeks;

/// <summary>API-facing mirror of <see cref="HomeTaskManager.Domain.Entities.TaskInstance"/>; Original* fields are populated only for carry-over instances (ADR-005).</summary>
public sealed record TaskInstanceDto(
    Guid Id,
    Guid TaskTemplateId,
    Guid WeekId,
    DateOnly PlannedDate,
    DateOnly DueDate,
    string Status,
    bool IsCompleted,
    DateTime? CompletedAt,
    bool IsCarryOver,
    Guid? AssignedUserId,
    Guid? ParentTaskInstanceId = null,
    int? OriginalWeekYear = null,
    int? OriginalWeekNumber = null,
    DateOnly? OriginalDueDate = null);

/// <summary>API-facing mirror of <see cref="HomeTaskManager.Domain.Entities.Week"/>.</summary>
public sealed record WeekDto(
    Guid Id,
    int Year,
    int WeekNumber,
    DateOnly StartDate,
    DateOnly EndDate,
    string Status,
    int SegmentNumber = 1,
    bool IgnoreHistoryForAssignment = false,
    DateTime? StartsAt = null,
    DateTime? EndsAt = null,
    DateTime? ClosedAt = null);
