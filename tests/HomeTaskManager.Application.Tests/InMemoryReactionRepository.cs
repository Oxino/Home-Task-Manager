using HomeTaskManager.Application.Abstractions.Repositories;
using HomeTaskManager.Domain.Entities;

namespace HomeTaskManager.Application.Tests;

/// <summary>In-memory fake standing in for the EF-backed Reaction store in tests.</summary>
internal sealed class InMemoryReactionRepository : IReactionRepository
{
    private readonly List<Reaction> _reactions = [];

    public Task<Reaction?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_reactions.FirstOrDefault(r => r.Id == id));

    public Task<IReadOnlyList<Reaction>> GetByCommentIdAsync(Guid commentId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Reaction>>(_reactions.Where(r => r.CommentId == commentId).ToList());

    public Task<Reaction?> GetByCommentAndUserAndTypeAsync(Guid commentId, Guid userId, string type, CancellationToken cancellationToken = default) =>
        Task.FromResult(_reactions.FirstOrDefault(r => r.CommentId == commentId && r.UserId == userId && r.ReactionType == type));

    public Task AddAsync(Reaction reaction, CancellationToken cancellationToken = default)
    {
        _reactions.Add(reaction);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Reaction reaction, CancellationToken cancellationToken = default)
    {
        _reactions.Remove(reaction);
        return Task.CompletedTask;
    }
}
