using HomeTaskManager.Domain.Exceptions;

namespace HomeTaskManager.Domain.Entities;

public sealed class UserPreferences
{
    private UserPreferences() { }

    public UserPreferences(Guid userId)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        NotificationsEnabled = true;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = CreatedAt;
    }

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public bool NotificationsEnabled { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public void SetNotificationsEnabled(bool enabled)
    {
        NotificationsEnabled = enabled;
        UpdatedAt = DateTime.UtcNow;
    }
}
