using HomeTaskManager.Domain.Exceptions;

namespace HomeTaskManager.Domain.Entities;

public sealed class Notification
{
    private Notification() { }

    public Notification(Guid userId, string title, string message, string? link = null)
    {
        if (string.IsNullOrWhiteSpace(title)) throw new DomainValidationException("A notification title is required.");
        if (string.IsNullOrWhiteSpace(message)) throw new DomainValidationException("A notification message is required.");
        Id = Guid.NewGuid();
        UserId = userId;
        Title = title.Trim();
        Message = message.Trim();
        Link = link;
        CreatedAt = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Message { get; private set; } = string.Empty;
    public string? Link { get; private set; }
    public bool IsRead { get; private set; }
    public DateTime? ReadAt { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public void MarkAsRead()
    {
        IsRead = true;
        ReadAt ??= DateTime.UtcNow;
    }
}