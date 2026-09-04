using HomeTaskManager.Domain.Entities;
using HomeTaskManager.Domain.ValueObjects;

namespace HomeTaskManager.Application.Abstractions.Repositories;

/// <summary>Persists/retrieves <see cref="Week"/> records (ISO week + its segments).</summary>
public interface IWeekRepository
{
    Task<Week?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Week?> GetByKeyAsync(WeekKey key, CancellationToken cancellationToken = default);

    /// <summary>Next SegmentNumber to use for a new segment of the same ISO week (e.g. after a mid-week split).</summary>
    Task<int> GetNextSegmentNumberAsync(WeekKey key, CancellationToken cancellationToken = default);

    Task AddAsync(Week week, CancellationToken cancellationToken = default);
}