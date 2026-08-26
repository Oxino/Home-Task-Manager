using HomeTaskManager.Domain.Entities;

namespace HomeTaskManager.Application.Abstractions.Repositories;

public interface IAuditLogRepository
{
    Task<IReadOnlyList<AuditLog>> GetByEntityAsync(string entityType, Guid entityId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AuditLog>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AuditLog>> GetByPerformerAsync(Guid performerId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AuditLog>> GetByPeriodAsync(DateTime from, DateTime to, string? action = null, CancellationToken cancellationToken = default);
}
