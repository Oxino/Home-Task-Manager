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

    public async Task<(IReadOnlyList<AuditLog> Items, int TotalCount)> GetPageAsync(Guid? performerId, string? category, string? entityType, string? action, string? sort, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = dbContext.AuditLogs.AsQueryable();
        if (performerId.HasValue) query = query.Where(entry => entry.PerformedByUserId == performerId.Value);
        if (!string.IsNullOrWhiteSpace(entityType)) query = query.Where(entry => entry.EntityType == entityType);
        if (!string.IsNullOrWhiteSpace(action)) query = query.Where(entry => entry.Action == action);
        if (category == "admin") query = query.Where(entry => entry.Action == "Reassigned" || entry.Action == "Cancelled" || entry.Action == "Executed" || entry.Action == "Generated" || entry.Action == "Closed" || entry.EntityType == "User" || entry.EntityType == "AssignmentEngineSettings" || (entry.Action == "Updated" && entry.EntityType == "TaskTemplate" && entry.NewValue != null && entry.NewValue.Contains("IsActive")));
        if (category == "user") query = query.Where(entry => entry.Action == "CompletionChanged" || entry.Action == "CommentAdded" || EF.Functions.Like(entry.EntityType, "%Preference%") || (entry.Action == "Updated" && entry.NewValue != null && (entry.NewValue.Contains("DateDisplayFormat") || entry.NewValue.Contains("NotificationsEnabled"))) || (entry.EntityType == "TaskTemplate" && entry.Action == "Created") || (entry.EntityType == "TaskTemplate" && entry.Action == "Updated" && entry.NewValue != null && !entry.NewValue.Contains("IsActive")));
        var totalCount = await query.CountAsync(cancellationToken);
        var orderedQuery = sort == "oldest" ? query.OrderBy(entry => entry.PerformedAt).ThenBy(entry => entry.Id) : query.OrderByDescending(entry => entry.PerformedAt).ThenByDescending(entry => entry.Id);
        var items = await orderedQuery
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return (items, totalCount);
    }
}
