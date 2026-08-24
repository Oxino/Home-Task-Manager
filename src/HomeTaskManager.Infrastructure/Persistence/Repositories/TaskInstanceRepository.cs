using HomeTaskManager.Application.Abstractions.Repositories;
using HomeTaskManager.Domain.Entities;
using HomeTaskManager.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace HomeTaskManager.Infrastructure.Persistence.Repositories;

public sealed class TaskInstanceRepository(HomeTaskManagerDbContext dbContext) : ITaskInstanceRepository
{
    public Task<TaskInstance?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.TaskInstances.SingleOrDefaultAsync(instance => instance.Id == id, cancellationToken);

    public async Task<IReadOnlyList<TaskInstance>> GetByWeekAsync(
        Guid weekId,
        CancellationToken cancellationToken = default) =>
        await dbContext.TaskInstances
            .Where(instance => instance.WeekId == weekId)
            .OrderBy(instance => instance.PlannedDate)
            .ToListAsync(cancellationToken);

    public Task<bool> ExistsForTaskTemplateAsync(Guid taskTemplateId, CancellationToken cancellationToken = default) =>
        dbContext.TaskInstances.AnyAsync(instance => instance.TaskTemplateId == taskTemplateId, cancellationToken);

    public async Task<IReadOnlyList<TaskInstance>> GetAssignedSinceAsync(
        DateOnly since,
        CancellationToken cancellationToken = default) =>
        await dbContext.TaskInstances
            .Where(instance => instance.AssignedUserId != null
                && instance.PlannedDate >= since
                && instance.Status != TaskInstanceStatus.Cancelled)
            .ToListAsync(cancellationToken);

    public Task AddRangeAsync(IEnumerable<TaskInstance> instances, CancellationToken cancellationToken = default)
    {
        return dbContext.TaskInstances.AddRangeAsync(instances, cancellationToken);
    }
}