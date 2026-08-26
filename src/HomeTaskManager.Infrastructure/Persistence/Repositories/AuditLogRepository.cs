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

    public async Task<IReadOnlyList<AuditLog>> GetByPeriodAsync(DateTime from, DateTime to, string? action = null, CancellationToken cancellationToken = default) =>
        await dbContext.AuditLogs
            .Where(entry => entry.PerformedAt >= from && entry.PerformedAt < to && (action == null || entry.Action == action))
            .OrderByDescending(entry => entry.PerformedAt)
            .ToListAsync(cancellationToken);

    public async Task<(IReadOnlyList<AuditLog> Items, int TotalCount)> GetPageAsync(Guid? performerId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = dbContext.AuditLogs.AsQueryable();
        if (performerId.HasValue) query = query.Where(entry => entry.PerformedByUserId == performerId.Value);
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(entry => entry.PerformedAt).ThenByDescending(entry => entry.Id)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return (items, totalCount);
    }
}
