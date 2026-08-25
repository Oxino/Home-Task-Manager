namespace HomeTaskManager.Application.Abstractions;

public interface INotificationService
{
    Task SendReminderNotificationAsync(Guid userId, string title, string message, CancellationToken cancellationToken = default);
    Task SendAssignmentNotificationAsync(Guid userId, string taskTitle, string weekInfo, CancellationToken cancellationToken = default);
}
