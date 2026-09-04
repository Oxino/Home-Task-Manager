using HomeTaskManager.Application.Abstractions.Repositories;
using HomeTaskManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HomeTaskManager.Infrastructure.Persistence.Repositories;

/// <summary>EF Core implementation of <see cref="IUserPreferencesRepository"/>.</summary>
public sealed class UserPreferencesRepository(HomeTaskManagerDbContext dbContext) : IUserPreferencesRepository
{
    public Task<UserPreferences?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default) => dbContext.UserPreferences.SingleOrDefaultAsync(preferences => preferences.UserId == userId, cancellationToken);
    public Task AddAsync(UserPreferences preferences, CancellationToken cancellationToken = default) => dbContext.UserPreferences.AddAsync(preferences, cancellationToken).AsTask();
}