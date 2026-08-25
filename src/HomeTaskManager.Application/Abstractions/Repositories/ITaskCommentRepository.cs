using HomeTaskManager.Domain.Entities;

namespace HomeTaskManager.Application.Abstractions.Repositories;

public interface ITaskCommentRepository
{
    Task<IReadOnlyList<TaskComment>> GetByTaskInstanceAsync(Guid taskInstanceId, CancellationToken cancellationToken = default);
    Task AddAsync(TaskComment comment, CancellationToken cancellationToken = default);
}