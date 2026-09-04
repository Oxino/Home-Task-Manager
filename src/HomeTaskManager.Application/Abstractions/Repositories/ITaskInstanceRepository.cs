using HomeTaskManager.Domain.Entities;

namespace HomeTaskManager.Application.Abstractions.Repositories;

/// <summary>Persists/retrieves <see cref="TaskInstance"/> records (concrete weekly occurrences of task templates).</summary>
public interface ITaskInstanceRepository
{
    Task<TaskInstance?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TaskInstance>> GetByWeekAsync(Guid weekId, CancellationToken cancellationToken = default);

    /// <summary>Used before deleting a template to guard against orphaning instances already generated from it.</summary>
    Task<bool> ExistsForTaskTemplateAsync(Guid taskTemplateId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TaskInstance>> GetAssignedSinceAsync(DateOnly since, CancellationToken cancellationToken = default);

    Task AddRangeAsync(IEnumerable<TaskInstance> instances, CancellationToken cancellationToken = default);

    void RemoveRange(IEnumerable<TaskInstance> instances);
}