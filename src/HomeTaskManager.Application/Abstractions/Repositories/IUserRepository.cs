using HomeTaskManager.Domain.Entities;

namespace HomeTaskManager.Application.Abstractions.Repositories;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<User>> GetActiveAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<bool> ExistsByDisplayNameAsync(string displayName, CancellationToken cancellationToken = default);

    Task<bool> ExistsAnyAdminAsync(CancellationToken cancellationToken = default);

    Task AddAsync(User user, CancellationToken cancellationToken = default);
}