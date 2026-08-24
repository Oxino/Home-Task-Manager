using HomeTaskManager.Application.Abstractions.Repositories;
using HomeTaskManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HomeTaskManager.Infrastructure.Persistence.Repositories;

public sealed class AssignmentRunRepository(HomeTaskManagerDbContext dbContext) : IAssignmentRunRepository
{
    public Task<bool> ExistsForWeekAsync(Guid weekId, CancellationToken cancellationToken = default) =>
        dbContext.AssignmentRuns.AnyAsync(run => run.WeekId == weekId, cancellationToken);

    public Task AddAsync(AssignmentRun assignmentRun, CancellationToken cancellationToken = default)
    {
        return dbContext.AssignmentRuns.AddAsync(assignmentRun, cancellationToken).AsTask();
    }
}
