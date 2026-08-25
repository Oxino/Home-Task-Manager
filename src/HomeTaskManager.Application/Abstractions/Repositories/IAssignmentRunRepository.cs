using HomeTaskManager.Domain.Entities;

namespace HomeTaskManager.Application.Abstractions.Repositories;

public interface IAssignmentRunRepository
{
    Task<bool> ExistsForWeekAsync(Guid weekId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AssignmentRun>> GetByWeekAsync(Guid weekId, CancellationToken cancellationToken = default);

    Task AddAsync(AssignmentRun assignmentRun, CancellationToken cancellationToken = default);
}