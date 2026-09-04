using HomeTaskManager.Application.Abstractions;

namespace HomeTaskManager.Application.Tests;

/// <summary>No-op INotificationService fake: tests assert on the persisted Notification entity instead of the delivery channel.</summary>
internal sealed class InMemoryNotificationService : INotificationService
{
    public Task SendReminderNotificationAsync(Guid userId, string title, string message, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task SendAssignmentNotificationAsync(Guid userId, string taskTitle, string weekInfo, CancellationToken cancellationToken = default) => Task.CompletedTask;
}
