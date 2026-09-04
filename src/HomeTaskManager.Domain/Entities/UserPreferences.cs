using HomeTaskManager.Domain.Exceptions;

namespace HomeTaskManager.Domain.Entities;

/// <summary>Per-user display/notification settings (one row per <see cref="User"/>), separate from the shared engine <see cref="SystemSetting"/>s.</summary>
public sealed class UserPreferences
{
    private UserPreferences() { }

    public UserPreferences(Guid userId)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        NotificationPreference = "All";
        DateDisplayFormat = "Weekday";
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = CreatedAt;
    }

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string NotificationPreference { get; private set; } = "All";
    public string DateDisplayFormat { get; private set; } = "Weekday";
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public void SetNotificationPreference(string preference)
    {
        if (preference is not ("None" or "Important" or "All"))
            throw new DomainValidationException("Unsupported notification preference.");
        NotificationPreference = preference;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetDateDisplayFormat(string format)
    {
        if (format is not ("Weekday" or "Numeric")) throw new DomainValidationException("Unsupported date display format.");
        DateDisplayFormat = format;
        UpdatedAt = DateTime.UtcNow;
    }
}
