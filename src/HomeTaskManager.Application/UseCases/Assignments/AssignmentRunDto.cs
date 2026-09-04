namespace HomeTaskManager.Application.UseCases.Assignments;

/// <summary>API-facing mirror of <see cref="HomeTaskManager.Domain.Entities.AssignmentRun"/>.</summary>
public sealed record AssignmentRunDto(
    Guid Id,
    Guid WeekId,
    string StrategyName,
    Guid TriggeredByUserId,
    bool IsAutomatic,
    string? Notes,
    DateTime GeneratedAt);
