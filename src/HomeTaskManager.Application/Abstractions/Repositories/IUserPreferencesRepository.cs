using HomeTaskManager.Domain.Entities;

namespace HomeTaskManager.Application.Abstractions.Repositories;

public interface IUserPreferencesRepository
{
    Task<UserPreferences?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task AddAsync(UserPreferences preferences, CancellationToken cancellationToken = default);
}