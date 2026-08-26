using HomeTaskManager.Domain.Entities;

namespace HomeTaskManager.Application.Abstractions.Repositories;

public interface ITaskInstanceRepository
{
    Task<TaskInstance?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TaskInstance>> GetByWeekAsync(Guid weekId, CancellationToken cancellationToken = default);

    Task<bool> ExistsForTaskTemplateAsync(Guid taskTemplateId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TaskInstance>> GetAssignedSinceAsync(DateOnly since, CancellationToken cancellationToken = default);

    Task AddRangeAsync(IEnumerable<TaskInstance> instances, CancellationToken cancellationToken = default);

    void RemoveRange(IEnumerable<TaskInstance> instances);
}