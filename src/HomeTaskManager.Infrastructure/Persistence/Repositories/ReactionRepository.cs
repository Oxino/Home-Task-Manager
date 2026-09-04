using HomeTaskManager.Application.Abstractions.Repositories;
using HomeTaskManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HomeTaskManager.Infrastructure.Persistence.Repositories;

/// <summary>EF Core implementation of <see cref="IReactionRepository"/>.</summary>
internal sealed class ReactionRepository(HomeTaskManagerDbContext dbContext) : IReactionRepository
{
    public async Task<Reaction?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await dbContext.Set<Reaction>().FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Reaction>> GetByCommentIdAsync(Guid commentId, CancellationToken cancellationToken = default) =>
        await dbContext.Set<Reaction>().Where(r => r.CommentId == commentId).ToListAsync(cancellationToken);

    public async Task<Reaction?> GetByCommentAndUserAndTypeAsync(Guid commentId, Guid userId, string type, CancellationToken cancellationToken = default) =>
        await dbContext.Set<Reaction>().FirstOrDefaultAsync(r => r.CommentId == commentId && r.UserId == userId && r.ReactionType == type, cancellationToken);

    public async Task AddAsync(Reaction reaction, CancellationToken cancellationToken = default)
    {
        await dbContext.Set<Reaction>().AddAsync(reaction, cancellationToken);
    }

    public Task DeleteAsync(Reaction reaction, CancellationToken cancellationToken = default)
    {
        dbContext.Set<Reaction>().Remove(reaction);
        return Task.CompletedTask;
    }
}
