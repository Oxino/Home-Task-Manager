using HomeTaskManager.Application.Abstractions;

namespace HomeTaskManager.Infrastructure.Persistence;

/// <summary>EF Core-backed <see cref="IUnitOfWork"/>: commits every tracked change (across all repositories sharing this DbContext) as one transaction.</summary>
public sealed class EfUnitOfWork(HomeTaskManagerDbContext dbContext) : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        dbContext.SaveChangesAsync(cancellationToken);
}