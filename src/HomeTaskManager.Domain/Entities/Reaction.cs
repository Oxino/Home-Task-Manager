namespace HomeTaskManager.Domain.Entities;

/// <summary>A single user's reaction (e.g. emoji) to a <see cref="TaskComment"/>.</summary>
public sealed class Reaction
{
    public Reaction(Guid commentId, Guid userId, string reactionType)
    {
        Id = Guid.NewGuid();
        CommentId = commentId;
        UserId = userId;
        ReactionType = reactionType;
        CreatedAt = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid CommentId { get; private set; }
    public Guid UserId { get; private set; }
    public string ReactionType { get; private set; }
    public DateTime CreatedAt { get; private set; }
}
