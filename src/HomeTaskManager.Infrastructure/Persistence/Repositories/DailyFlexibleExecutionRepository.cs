using HomeTaskManager.Application.Abstractions.Repositories;
using HomeTaskManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HomeTaskManager.Infrastructure.Persistence.Repositories;

public sealed class DailyFlexibleExecutionRepository(HomeTaskManagerDbContext dbContext) : IDailyFlexibleExecutionRepository
{
    public async Task<IReadOnlyList<DailyFlexibleExecution>> GetByTaskInstanceAsync(Guid taskInstanceId, CancellationToken cancellationToken = default) => await dbContext.DailyFlexibleExecutions.Where(execution => execution.TaskInstanceId == taskInstanceId).OrderBy(execution => execution.ExecutionDate).ToListAsync(cancellationToken);
    public Task AddAsync(DailyFlexibleExecution execution, CancellationToken cancellationToken = default) => dbContext.DailyFlexibleExecutions.AddAsync(execution, cancellationToken).AsTask();
}