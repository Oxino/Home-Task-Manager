using HomeTaskManager.Domain.Exceptions;

namespace HomeTaskManager.Domain.Entities;

/// <summary>Immutable trace of a change made to any entity (who/what/when/before/after). Never purged (ADR-004, BR-057).</summary>
public sealed class AuditLog
{
    private AuditLog()
    {
    }

    public AuditLog(string entityType, Guid entityId, string action, Guid performedByUserId, string? oldValue, string? newValue)
        : this(entityType, entityId, action, performedByUserId, oldValue, newValue, DateTime.UtcNow)
    {
    }

    /// <param name="performedAt">Explicit timestamp overload, used mainly by tests/seed data; production code should use the 6-argument constructor which stamps UtcNow.</param>
    public AuditLog(string entityType, Guid entityId, string action, Guid performedByUserId, string? oldValue, string? newValue, DateTime performedAt)
    {
        if (string.IsNullOrWhiteSpace(entityType))
        {
            throw new DomainValidationException("An audit log entry requires an entity type.");
        }

        if (string.IsNullOrWhiteSpace(action))
        {
            throw new DomainValidationException("An audit log entry requires an action.");
        }

        Id = Guid.NewGuid();
        EntityType = entityType;
        EntityId = entityId;
        Action = action;
        PerformedByUserId = performedByUserId;
        OldValue = oldValue;
        NewValue = newValue;
        PerformedAt = performedAt;
    }

    public Guid Id { get; private set; }

    public string EntityType { get; private set; } = string.Empty;

    public Guid EntityId { get; private set; }

    public string Action { get; private set; } = string.Empty;

    public Guid PerformedByUserId { get; private set; }

    public string? OldValue { get; private set; }

    public string? NewValue { get; private set; }

    public DateTime PerformedAt { get; private set; }
}
