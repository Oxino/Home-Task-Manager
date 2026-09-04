using HomeTaskManager.Domain.Entities;

namespace HomeTaskManager.Application.Abstractions.Repositories;

/// <summary>Persists/retrieves <see cref="Reaction"/>s (e.g. emoji) attached to task comments.</summary>
public interface IReactionRepository
{
    Task<Reaction?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Reaction>> GetByCommentIdAsync(Guid commentId, CancellationToken cancellationToken = default);
    Task<Reaction?> GetByCommentAndUserAndTypeAsync(Guid commentId, Guid userId, string type, CancellationToken cancellationToken = default);
    Task AddAsync(Reaction reaction, CancellationToken cancellationToken = default);
    Task DeleteAsync(Reaction reaction, CancellationToken cancellationToken = default);
}
