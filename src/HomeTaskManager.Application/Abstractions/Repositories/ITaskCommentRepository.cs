using HomeTaskManager.Domain.Entities;

namespace HomeTaskManager.Application.Abstractions.Repositories;

/// <summary>Persists/retrieves <see cref="TaskComment"/>s (threaded discussion on a task instance).</summary>
public interface ITaskCommentRepository
{
    Task<IReadOnlyList<TaskComment>> GetByTaskInstanceAsync(Guid taskInstanceId, CancellationToken cancellationToken = default);
    Task AddAsync(TaskComment comment, CancellationToken cancellationToken = default);
}