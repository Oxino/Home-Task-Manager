namespace HomeTaskManager.Application.UseCases.Audit;

public sealed record AuditLogDto(
    Guid Id,
    string EntityType,
    Guid EntityId,
    string Action,
    Guid PerformedByUserId,
    string? OldValue,
    string? NewValue,
    DateTime PerformedAt);
