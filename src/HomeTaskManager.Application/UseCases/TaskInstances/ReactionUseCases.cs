using HomeTaskManager.Application.Abstractions;
using HomeTaskManager.Application.Abstractions.Repositories;
using HomeTaskManager.Domain.Entities;

namespace HomeTaskManager.Application.UseCases.TaskInstances;

/// <summary>Adds a reaction to a comment, idempotently (returns the existing one if the same user/type pair already reacted).</summary>
public interface IAddReactionUseCase
{
    Task<ReactionDto> ExecuteAsync(Guid commentId, Guid userId, string reactionType, CancellationToken cancellationToken = default);
}

public interface IRemoveReactionUseCase
{
    Task ExecuteAsync(Guid reactionId, CancellationToken cancellationToken = default);
}

public interface IListReactionsForCommentUseCase
{
    Task<List<ReactionDto>> ExecuteAsync(Guid commentId, CancellationToken cancellationToken = default);
}

public sealed record ReactionDto(Guid Id, Guid CommentId, Guid UserId, string ReactionType, DateTime CreatedAt);

public sealed class AddReactionUseCase(IReactionRepository reactionRepository, IUnitOfWork unitOfWork) : IAddReactionUseCase
{
    public async Task<ReactionDto> ExecuteAsync(Guid commentId, Guid userId, string reactionType, CancellationToken cancellationToken = default)
    {
        // Same user reacting twice with the same type is a no-op, not a duplicate row.
        var existing = await reactionRepository.GetByCommentAndUserAndTypeAsync(commentId, userId, reactionType, cancellationToken);
        if (existing is not null) return new ReactionDto(existing.Id, existing.CommentId, existing.UserId, existing.ReactionType, existing.CreatedAt);

        var reaction = new Reaction(commentId, userId, reactionType);
        await reactionRepository.AddAsync(reaction, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new ReactionDto(reaction.Id, reaction.CommentId, reaction.UserId, reaction.ReactionType, reaction.CreatedAt);
    }
}

public sealed class RemoveReactionUseCase(IReactionRepository reactionRepository, IUnitOfWork unitOfWork) : IRemoveReactionUseCase
{
    public async Task ExecuteAsync(Guid reactionId, CancellationToken cancellationToken = default)
    {
        var reaction = await reactionRepository.GetByIdAsync(reactionId, cancellationToken);
        if (reaction is null) return;

        await reactionRepository.DeleteAsync(reaction, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

public sealed class ListReactionsForCommentUseCase(IReactionRepository reactionRepository) : IListReactionsForCommentUseCase
{
    public async Task<List<ReactionDto>> ExecuteAsync(Guid commentId, CancellationToken cancellationToken = default)
    {
        var reactions = await reactionRepository.GetByCommentIdAsync(commentId, cancellationToken);
        return reactions.Select(r => new ReactionDto(r.Id, r.CommentId, r.UserId, r.ReactionType, r.CreatedAt)).ToList();
    }
}
