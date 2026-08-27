using HomeTaskManager.Domain.Enums;

namespace HomeTaskManager.Application.Abstractions;

public interface INotificationService
{
    async Task SendNotificationAsync(Guid userId, string title, string message, NotificationImportance importance, CancellationToken cancellationToken = default)
    {
        if (importance == NotificationImportance.Important)
        {
            await SendAssignmentNotificationAsync(userId, title, message, cancellationToken);
            return;
        }

        await SendReminderNotificationAsync(userId, title, message, cancellationToken);
    }

    Task SendReminderNotificationAsync(Guid userId, string title, string message, CancellationToken cancellationToken = default);
    Task SendAssignmentNotificationAsync(Guid userId, string taskTitle, string weekInfo, CancellationToken cancellationToken = default);
}
