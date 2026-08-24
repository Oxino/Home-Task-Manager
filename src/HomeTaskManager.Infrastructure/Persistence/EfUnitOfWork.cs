using HomeTaskManager.Application.Abstractions;

namespace HomeTaskManager.Infrastructure.Persistence;

public sealed class EfUnitOfWork(HomeTaskManagerDbContext dbContext) : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        dbContext.SaveChangesAsync(cancellationToken);
}