using HomeTaskManager.Domain.Entities;

namespace HomeTaskManager.Application.Abstractions.Repositories;

public interface IAssignmentExplanationRepository
{
    Task<AssignmentExplanation?> GetByTaskInstanceIdAsync(Guid taskInstanceId, CancellationToken cancellationToken = default);
    Task RemoveByTaskInstanceIdsAsync(IEnumerable<Guid> taskInstanceIds, CancellationToken cancellationToken = default);
    Task AddAsync(AssignmentExplanation explanation, CancellationToken cancellationToken = default);
}
