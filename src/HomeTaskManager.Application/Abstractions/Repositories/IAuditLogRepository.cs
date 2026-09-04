using HomeTaskManager.Domain.Entities;

namespace HomeTaskManager.Application.Abstractions.Repositories;

/// <summary>Read access to the append-only <see cref="AuditLog"/> trail, including a filtered/sorted/paged query for the admin audit screen.</summary>
public interface IAuditLogRepository
{
    Task<IReadOnlyList<AuditLog>> GetByEntityAsync(string entityType, Guid entityId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AuditLog>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AuditLog>> GetByPerformerAsync(Guid performerId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AuditLog>> GetByPeriodAsync(DateTime from, DateTime to, string? action = null, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<AuditLog> Items, int TotalCount)> GetPageAsync(Guid? performerId, string? category, string? entityType, string? action, string? sort, int page, int pageSize, CancellationToken cancellationToken = default);
}
