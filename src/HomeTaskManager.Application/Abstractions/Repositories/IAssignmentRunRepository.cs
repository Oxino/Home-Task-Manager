using HomeTaskManager.Domain.Entities;

namespace HomeTaskManager.Application.Abstractions.Repositories;

/// <summary>Persists/retrieves <see cref="AssignmentRun"/> records (one per engine execution for a week).</summary>
public interface IAssignmentRunRepository
{
    Task<bool> ExistsForWeekAsync(Guid weekId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AssignmentRun>> GetByWeekAsync(Guid weekId, CancellationToken cancellationToken = default);

    Task AddAsync(AssignmentRun assignmentRun, CancellationToken cancellationToken = default);
}