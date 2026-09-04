using HomeTaskManager.Domain.Entities;

namespace HomeTaskManager.Application.Abstractions.Repositories;

/// <summary>Persists/retrieves <see cref="User"/> profiles (household members).</summary>
public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<User>> GetActiveAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<bool> ExistsByDisplayNameAsync(string displayName, CancellationToken cancellationToken = default);

    /// <summary>Used to gate the BR-017 bootstrap rule: an anonymous self-attributed admin is only allowed before any admin exists.</summary>
    Task<bool> ExistsAnyAdminAsync(CancellationToken cancellationToken = default);

    Task AddAsync(User user, CancellationToken cancellationToken = default);
}