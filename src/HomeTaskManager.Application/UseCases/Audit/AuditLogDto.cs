namespace HomeTaskManager.Application.UseCases.Audit;

/// <summary>API-facing mirror of <see cref="HomeTaskManager.Domain.Entities.AuditLog"/>.</summary>
public sealed record AuditLogDto(
    Guid Id,
    string EntityType,
    Guid EntityId,
    string Action,
    Guid PerformedByUserId,
    string? OldValue,
    string? NewValue,
    DateTime PerformedAt);
