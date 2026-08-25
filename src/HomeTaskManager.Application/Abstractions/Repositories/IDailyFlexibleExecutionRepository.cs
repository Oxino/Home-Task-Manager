using HomeTaskManager.Domain.Entities;

namespace HomeTaskManager.Application.Abstractions.Repositories;

public interface IDailyFlexibleExecutionRepository
{
    Task<IReadOnlyList<DailyFlexibleExecution>> GetByTaskInstanceAsync(Guid taskInstanceId, CancellationToken cancellationToken = default);
    Task AddAsync(DailyFlexibleExecution execution, CancellationToken cancellationToken = default);
}