using HomeTaskManager.Application.Abstractions;

namespace HomeTaskManager.Application.Tests;

internal sealed class InMemoryNotificationService : INotificationService
{
    public Task SendReminderNotificationAsync(Guid userId, string title, string message, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task SendAssignmentNotificationAsync(Guid userId, string taskTitle, string weekInfo, CancellationToken cancellationToken = default) => Task.CompletedTask;
}
