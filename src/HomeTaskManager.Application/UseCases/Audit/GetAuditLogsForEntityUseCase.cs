using HomeTaskManager.Application.Abstractions.Repositories;

namespace HomeTaskManager.Application.UseCases.Audit;

public interface IGetAuditLogsForEntityUseCase
{
    Task<IReadOnlyList<AuditLogDto>> ExecuteAsync(string entityType, Guid entityId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AuditLogDto>> ExecuteAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AuditLogDto>> ExecuteForPerformerAsync(Guid performerId, CancellationToken cancellationToken = default);
}

/// <summary>BR-033: exposes the audit trail of a given entity for explanation purposes.</summary>
public sealed class GetAuditLogsForEntityUseCase(IAuditLogRepository auditLogRepository) : IGetAuditLogsForEntityUseCase
{
    public async Task<IReadOnlyList<AuditLogDto>> ExecuteAsync(
        string entityType,
        Guid entityId,
        CancellationToken cancellationToken = default)
    {
        var entries = await auditLogRepository.GetByEntityAsync(entityType, entityId, cancellationToken);

        return Map(entries);
    }

    public async Task<IReadOnlyList<AuditLogDto>> ExecuteAllAsync(CancellationToken cancellationToken = default)
    {
        var entries = await auditLogRepository.GetAllAsync(cancellationToken);
        return Map(entries);
    }

    public async Task<IReadOnlyList<AuditLogDto>> ExecuteForPerformerAsync(Guid performerId, CancellationToken cancellationToken = default)
    {
        var entries = await auditLogRepository.GetByPerformerAsync(performerId, cancellationToken);
        return Map(entries);
    }

    private static IReadOnlyList<AuditLogDto> Map(IEnumerable<Domain.Entities.AuditLog> entries) => entries
        .Select(entry => new AuditLogDto(
            entry.Id, entry.EntityType, entry.EntityId, entry.Action, entry.PerformedByUserId,
            entry.OldValue, entry.NewValue, entry.PerformedAt))
        .ToList();
}
