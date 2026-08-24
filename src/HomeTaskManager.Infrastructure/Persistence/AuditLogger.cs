using HomeTaskManager.Application.Abstractions;
using HomeTaskManager.Domain.Entities;

namespace HomeTaskManager.Infrastructure.Persistence;

/// <summary>Adds the entry to the tracked change set; persisted together with the triggering use case's SaveChanges.</summary>
public sealed class AuditLogger(HomeTaskManagerDbContext dbContext) : IAuditLogger
{
    public async Task LogAsync(
        string entityType,
        Guid entityId,
        string action,
        Guid performedByUserId,
        string? oldValue = null,
        string? newValue = null,
        CancellationToken cancellationToken = default)
    {
        var entry = new AuditLog(entityType, entityId, action, performedByUserId, oldValue, newValue);
        await dbContext.AuditLogs.AddAsync(entry, cancellationToken);
    }
}
