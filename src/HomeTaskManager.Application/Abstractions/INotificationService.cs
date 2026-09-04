using HomeTaskManager.Domain.Enums;

namespace HomeTaskManager.Application.Abstractions;

/// <summary>Sends in-app notifications; the default method routes by importance so callers don't need to pick the channel themselves.</summary>
public interface INotificationService
{
    /// <summary>Default dispatch: Important notifications go through the assignment channel, everything else through the reminder channel.</summary>
    async Task SendNotificationAsync(Guid userId, string title, string message, NotificationImportance importance, CancellationToken cancellationToken = default)
    {
        if (importance == NotificationImportance.Important)
        {
            await SendAssignmentNotificationAsync(userId, title, message, cancellationToken);
            return;
        }

        await SendReminderNotificationAsync(userId, title, message, cancellationToken);
    }

    /// <summary>Low-priority notification (e.g. a reminder before a due date), filtered out for users who only want "Important".</summary>
    Task SendReminderNotificationAsync(Guid userId, string title, string message, CancellationToken cancellationToken = default);
    /// <summary>High-priority notification (e.g. a new task assignment), always delivered unless the user opted for "None".</summary>
    Task SendAssignmentNotificationAsync(Guid userId, string taskTitle, string weekInfo, CancellationToken cancellationToken = default);
}
