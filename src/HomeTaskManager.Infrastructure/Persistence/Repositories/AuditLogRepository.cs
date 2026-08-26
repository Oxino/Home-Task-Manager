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

    public async Task<IReadOnlyList<AuditLog>> GetByPerformerAsync(Guid performerId, CancellationToken cancellationToken = default) =>
        await dbContext.AuditLogs
            .Where(entry => entry.PerformedByUserId == performerId)
            .OrderByDescending(entry => entry.PerformedAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<AuditLog>> GetByPeriodAsync(DateTime from, DateTime to, CancellationToken cancellationToken = default) =>
        await dbContext.AuditLogs
            .Where(entry => entry.PerformedAt >= from && entry.PerformedAt < to)
            .OrderByDescending(entry => entry.PerformedAt)
            .ToListAsync(cancellationToken);
}
