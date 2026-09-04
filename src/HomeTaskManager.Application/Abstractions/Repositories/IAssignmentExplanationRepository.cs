using HomeTaskManager.Domain.Entities;

namespace HomeTaskManager.Application.Abstractions.Repositories;

/// <summary>Persists/retrieves the BR-033 decision trace (<see cref="AssignmentExplanation"/>) behind each task instance's assignment.</summary>
public interface IAssignmentExplanationRepository
{
    Task<AssignmentExplanation?> GetByTaskInstanceIdAsync(Guid taskInstanceId, CancellationToken cancellationToken = default);
    Task RemoveByTaskInstanceIdsAsync(IEnumerable<Guid> taskInstanceIds, CancellationToken cancellationToken = default);
    Task AddAsync(AssignmentExplanation explanation, CancellationToken cancellationToken = default);
}
