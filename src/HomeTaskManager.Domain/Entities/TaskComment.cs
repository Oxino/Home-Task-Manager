using HomeTaskManager.Domain.Exceptions;

namespace HomeTaskManager.Domain.Entities;

public sealed class TaskComment
{
    private TaskComment() { }

    public TaskComment(Guid taskInstanceId, Guid authorUserId, string content)
    {
        if (string.IsNullOrWhiteSpace(content)) throw new DomainValidationException("A comment requires content.");
        Id = Guid.NewGuid();
        TaskInstanceId = taskInstanceId;
        AuthorUserId = authorUserId;
        Content = content.Trim();
        CreatedAt = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid TaskInstanceId { get; private set; }
    public Guid AuthorUserId { get; private set; }
    public string Content { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }
}
