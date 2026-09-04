using HomeTaskManager.Application.Abstractions.Repositories;
using HomeTaskManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HomeTaskManager.Infrastructure.Persistence.Repositories;

/// <summary>EF Core implementation of <see cref="ITaskTemplateRepository"/>.</summary>
public sealed class TaskTemplateRepository(HomeTaskManagerDbContext dbContext) : ITaskTemplateRepository
{
    public Task<TaskTemplate?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.TaskTemplates.SingleOrDefaultAsync(task => task.Id == id, cancellationToken);

    public async Task<IReadOnlyList<TaskTemplate>> GetActiveAsync(CancellationToken cancellationToken = default) =>
        await dbContext.TaskTemplates
            .Where(task => task.IsActive)
            .OrderBy(task => task.Title)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<TaskTemplate>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await dbContext.TaskTemplates.OrderBy(task => task.Title).ToListAsync(cancellationToken);

    // Case-insensitive comparison since the unique DB index on Title is case-sensitive by default (SQLite/Postgres).
    public Task<bool> ExistsByTitleAsync(string title, CancellationToken cancellationToken = default) =>
        dbContext.TaskTemplates.AnyAsync(task => task.Title.ToLower() == title.ToLower(), cancellationToken);

    public Task<bool> ExistsByTitleAsync(string title, Guid excludingId, CancellationToken cancellationToken = default) =>
        dbContext.TaskTemplates.AnyAsync(task => task.Id != excludingId && task.Title.ToLower() == title.ToLower(), cancellationToken);

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