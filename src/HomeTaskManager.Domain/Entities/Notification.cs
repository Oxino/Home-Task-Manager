using HomeTaskManager.Domain.Exceptions;
using HomeTaskManager.Domain.Enums;

namespace HomeTaskManager.Domain.Entities;

/// <summary>An in-app notification shown to a single user (reminders, reassignments, etc.), with read/unread state.</summary>
public sealed class Notification
{
    private Notification() { }

    public Notification(Guid userId, string title, string message, string? link = null, NotificationImportance importance = NotificationImportance.Normal)
    {
        if (string.IsNullOrWhiteSpace(title)) throw new DomainValidationException("A notification title is required.");
        if (string.IsNullOrWhiteSpace(message)) throw new DomainValidationException("A notification message is required.");
        Id = Guid.NewGuid();
        UserId = userId;
        Title = title.Trim();
        Message = message.Trim();
        Link = link;
        Importance = importance;
        CreatedAt = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Message { get; private set; } = string.Empty;
    public string? Link { get; private set; }
    public NotificationImportance Importance { get; private set; }
    public bool IsRead { get; private set; }
    public DateTime? ReadAt { get; private set; }
    public DateTime CreatedAt { get; private set; }

    /// <summary>Idempotent: calling this on an already-read notification keeps the original ReadAt timestamp.</summary>
    public void MarkAsRead()
    {
        IsRead = true;
        ReadAt ??= DateTime.UtcNow;
    }

    public void SetLink(string? link)
    {
        Link = string.IsNullOrWhiteSpace(link) ? null : link.Trim();
    }
}