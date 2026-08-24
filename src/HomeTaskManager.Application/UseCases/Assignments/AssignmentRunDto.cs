namespace HomeTaskManager.Application.UseCases.Assignments;

public sealed record AssignmentRunDto(
    Guid Id,
    Guid WeekId,
    string StrategyName,
    Guid TriggeredByUserId,
    bool IsAutomatic,
    string? Notes,
    DateTime GeneratedAt);
