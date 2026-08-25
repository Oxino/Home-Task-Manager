using HomeTaskManager.Application.Abstractions.Repositories;
using HomeTaskManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HomeTaskManager.Infrastructure.Persistence.Repositories;

public sealed class AuditLogRepository(HomeTaskManagerDbContext dbContext) : IAuditLogRepository
{
    public async Task<IReadOnlyList<AuditLog>> GetByEntityAsync(
        string entityType,
        Guid entityId,
        CancellationToken cancellationToken = default) =>
        await dbContext.AuditLogs
            .Where(entry => entry.EntityType == entityType && entry.EntityId == entityId)
            .OrderBy(entry => entry.PerformedAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<AuditLog>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await dbContext.AuditLogs
            .OrderByDescending(entry => entry.PerformedAt)
            .ToListAsync(cancellationToken);
}
