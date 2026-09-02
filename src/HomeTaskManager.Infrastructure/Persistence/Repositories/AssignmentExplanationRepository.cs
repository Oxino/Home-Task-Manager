using HomeTaskManager.Application.Abstractions.Repositories;
using HomeTaskManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HomeTaskManager.Infrastructure.Persistence.Repositories;

internal sealed class AssignmentExplanationRepository(HomeTaskManagerDbContext dbContext) : IAssignmentExplanationRepository
{
    public async Task<AssignmentExplanation?> GetByTaskInstanceIdAsync(Guid taskInstanceId, CancellationToken cancellationToken = default) =>
        await dbContext.Set<AssignmentExplanation>().FirstOrDefaultAsync(e => e.TaskInstanceId == taskInstanceId, cancellationToken);

    public async Task RemoveByTaskInstanceIdsAsync(IEnumerable<Guid> taskInstanceIds, CancellationToken cancellationToken = default)
    {
        var existing = await dbContext.Set<AssignmentExplanation>()
            .Where(e => taskInstanceIds.Contains(e.TaskInstanceId))
            .ToListAsync(cancellationToken);
        dbContext.Set<AssignmentExplanation>().RemoveRange(existing);
    }

    public async Task AddAsync(AssignmentExplanation explanation, CancellationToken cancellationToken = default) =>
        await dbContext.Set<AssignmentExplanation>().AddAsync(explanation, cancellationToken);
}
