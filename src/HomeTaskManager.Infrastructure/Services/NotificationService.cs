using HomeTaskManager.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace HomeTaskManager.Infrastructure.Services;

/// <summary>Placeholder <see cref="INotificationService"/>: only logs (no email/push/etc.), since notification delivery is out of scope for V1 (in-app notifications cover the current need).</summary>
public sealed class NotificationService(ILogger<NotificationService> logger) : INotificationService
{
    public Task SendReminderNotificationAsync(Guid userId, string title, string message, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Sending reminder notification to user {UserId}: {Title} - {Message}", userId, title, message);
        return Task.CompletedTask;
    }

    public Task SendAssignmentNotificationAsync(Guid userId, string taskTitle, string weekInfo, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Sending assignment notification to user {UserId}: {TaskTitle} for week {WeekInfo}", userId, taskTitle, weekInfo);
        return Task.CompletedTask;
    }
}
