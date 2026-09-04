using HomeTaskManager.Domain.Exceptions;

namespace HomeTaskManager.Domain.Entities;

/// <summary>A user comment on a <see cref="TaskInstance"/>, optionally a reply to another comment via ParentCommentId (threaded discussion).</summary>
public sealed class TaskComment
{
    private TaskComment() { }

    public TaskComment(Guid taskInstanceId, Guid authorUserId, string content, Guid? parentCommentId = null)
    {
        if (string.IsNullOrWhiteSpace(content)) throw new DomainValidationException("A comment requires content.");
        Id = Guid.NewGuid();
        TaskInstanceId = taskInstanceId;
        AuthorUserId = authorUserId;
        Content = content.Trim();
        ParentCommentId = parentCommentId;
        CreatedAt = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid TaskInstanceId { get; private set; }
    public Guid AuthorUserId { get; private set; }
    public string Content { get; private set; } = string.Empty;
    public Guid? ParentCommentId { get; private set; }
    public DateTime CreatedAt { get; private set; }
}
