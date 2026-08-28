using HomeTaskManager.Domain.Entities;
using HomeTaskManager.Domain.ValueObjects;

namespace HomeTaskManager.Application.Abstractions.Repositories;

public interface IWeekRepository
{
    Task<Week?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Week?> GetByKeyAsync(WeekKey key, CancellationToken cancellationToken = default);

    Task<int> GetNextSegmentNumberAsync(WeekKey key, CancellationToken cancellationToken = default);

    Task AddAsync(Week week, CancellationToken cancellationToken = default);
}