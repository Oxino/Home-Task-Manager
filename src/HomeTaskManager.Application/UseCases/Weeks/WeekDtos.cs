namespace HomeTaskManager.Application.UseCases.Weeks;

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
    Guid? AssignedUserId);

public sealed record WeekDto(Guid Id, int Year, int WeekNumber, DateOnly StartDate, DateOnly EndDate, string Status);
