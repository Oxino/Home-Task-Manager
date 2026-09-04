using HomeTaskManager.Domain.Entities;

namespace HomeTaskManager.Application.Abstractions.Repositories;

/// <summary>Persists/retrieves <see cref="Reminder"/> rules attached to task templates.</summary>
public interface IReminderRepository
{
    Task<Reminder?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Reminder>> GetByTaskTemplateAsync(Guid taskTemplateId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Reminder>> GetEnabledAsync(CancellationToken cancellationToken = default);
    Task AddAsync(Reminder reminder, CancellationToken cancellationToken = default);
}