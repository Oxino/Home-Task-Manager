using HomeTaskManager.Application.Abstractions.Repositories;
using HomeTaskManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HomeTaskManager.Infrastructure.Persistence.Repositories;

public sealed class UserRepository(HomeTaskManagerDbContext dbContext) : IUserRepository
{
    public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.Users.SingleOrDefaultAsync(user => user.Id == id, cancellationToken);

    public async Task<IReadOnlyList<User>> GetActiveAsync(CancellationToken cancellationToken = default) =>
        await dbContext.Users
            .Where(user => user.IsActive)
            .OrderBy(user => user.DisplayName)
            .ToListAsync(cancellationToken);

    public Task<bool> ExistsByDisplayNameAsync(string displayName, CancellationToken cancellationToken = default) =>
        dbContext.Users.AnyAsync(user => user.DisplayName == displayName, cancellationToken);

    public Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        return dbContext.Users.AddAsync(user, cancellationToken).AsTask();
    }
}