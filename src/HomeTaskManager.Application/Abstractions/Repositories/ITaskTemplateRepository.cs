using HomeTaskManager.Domain.Entities;

namespace HomeTaskManager.Application.Abstractions.Repositories;

/// <summary>Persists/retrieves <see cref="TaskTemplate"/>s (reusable task definitions).</summary>
public interface ITaskTemplateRepository
{
    Task<TaskTemplate?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TaskTemplate>> GetActiveAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TaskTemplate>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<bool> ExistsByTitleAsync(string title, CancellationToken cancellationToken = default);

    /// <summary>Overload used when updating a template, so it doesn't collide with its own current title.</summary>
    Task<bool> ExistsByTitleAsync(string title, Guid excludingId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TaskTemplate>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);

    Task AddAsync(TaskTemplate taskTemplate, CancellationToken cancellationToken = default);
}