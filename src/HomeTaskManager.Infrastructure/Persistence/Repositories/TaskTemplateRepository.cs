using HomeTaskManager.Application.Abstractions.Repositories;
using HomeTaskManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HomeTaskManager.Infrastructure.Persistence.Repositories;

public sealed class TaskTemplateRepository(HomeTaskManagerDbContext dbContext) : ITaskTemplateRepository
{
    public Task<TaskTemplate?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.TaskTemplates.SingleOrDefaultAsync(task => task.Id == id, cancellationToken);

    public async Task<IReadOnlyList<TaskTemplate>> GetActiveAsync(CancellationToken cancellationToken = default) =>
        await dbContext.TaskTemplates
            .Where(task => task.IsActive)
            .OrderBy(task => task.Title)
            .ToListAsync(cancellationToken);

    public Task<bool> ExistsByTitleAsync(string title, CancellationToken cancellationToken = default) =>
        dbContext.TaskTemplates.AnyAsync(task => task.Title == title, cancellationToken);

    public async Task<IReadOnlyList<TaskTemplate>> GetByIdsAsync(
        IEnumerable<Guid> ids,
        CancellationToken cancellationToken = default) =>
        await dbContext.TaskTemplates
            .Where(task => ids.Contains(task.Id))
            .ToListAsync(cancellationToken);

    public Task AddAsync(TaskTemplate taskTemplate, CancellationToken cancellationToken = default)
    {
        return dbContext.TaskTemplates.AddAsync(taskTemplate, cancellationToken).AsTask();
    }
}