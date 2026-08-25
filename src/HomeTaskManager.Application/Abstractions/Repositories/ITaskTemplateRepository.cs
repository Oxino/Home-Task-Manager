using HomeTaskManager.Domain.Entities;

namespace HomeTaskManager.Application.Abstractions.Repositories;

public interface ITaskTemplateRepository
{
    Task<TaskTemplate?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TaskTemplate>> GetActiveAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TaskTemplate>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<bool> ExistsByTitleAsync(string title, CancellationToken cancellationToken = default);

    Task<bool> ExistsByTitleAsync(string title, Guid excludingId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TaskTemplate>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);

    Task AddAsync(TaskTemplate taskTemplate, CancellationToken cancellationToken = default);
}