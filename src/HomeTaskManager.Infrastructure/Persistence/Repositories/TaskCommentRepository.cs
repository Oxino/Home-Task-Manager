using HomeTaskManager.Application.Abstractions.Repositories;
using HomeTaskManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HomeTaskManager.Infrastructure.Persistence.Repositories;

public sealed class TaskCommentRepository(HomeTaskManagerDbContext dbContext) : ITaskCommentRepository
{
    public async Task<IReadOnlyList<TaskComment>> GetByTaskInstanceAsync(Guid taskInstanceId, CancellationToken cancellationToken = default) =>
        await dbContext.TaskComments.Where(comment => comment.TaskInstanceId == taskInstanceId).OrderBy(comment => comment.CreatedAt).ToListAsync(cancellationToken);

    public Task AddAsync(TaskComment comment, CancellationToken cancellationToken = default) => dbContext.TaskComments.AddAsync(comment, cancellationToken).AsTask();
}