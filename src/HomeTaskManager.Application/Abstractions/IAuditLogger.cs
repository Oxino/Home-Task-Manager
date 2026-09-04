namespace HomeTaskManager.Application.Abstractions;

/// <summary>BR-017/BR-033: writes a generic, immutable trail for important business mutations.</summary>
public interface IAuditLogger
{
    /// <summary>Persists one immutable audit entry. Implementations should never throw for logging failures that would abort the caller's business operation.</summary>
    Task LogAsync(
        string entityType,
        Guid entityId,
        string action,
        Guid performedByUserId,
        string? oldValue = null,
        string? newValue = null,
        CancellationToken cancellationToken = default);
}
