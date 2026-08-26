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
        DateDisplayFormat = "Weekday";
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = CreatedAt;
    }

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public bool NotificationsEnabled { get; private set; }
    public string DateDisplayFormat { get; private set; } = "Weekday";
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public void SetNotificationsEnabled(bool enabled)
    {
        NotificationsEnabled = enabled;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetDateDisplayFormat(string format)
    {
        if (format is not ("Weekday" or "Numeric")) throw new DomainValidationException("Unsupported date display format.");
        DateDisplayFormat = format;
        UpdatedAt = DateTime.UtcNow;
    }
}
