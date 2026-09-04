using HomeTaskManager.Domain.Entities;

namespace HomeTaskManager.Application.Abstractions.Repositories;

/// <summary>Persists/retrieves logged executions of "daily flexible" tasks (<see cref="DailyFlexibleExecution"/>).</summary>
public interface IDailyFlexibleExecutionRepository
{
    Task<IReadOnlyList<DailyFlexibleExecution>> GetByTaskInstanceAsync(Guid taskInstanceId, CancellationToken cancellationToken = default);
    Task AddAsync(DailyFlexibleExecution execution, CancellationToken cancellationToken = default);
}