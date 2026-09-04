using HomeTaskManager.Application.UseCases.Notifications;

/// <summary>Hosted service that periodically runs <see cref="IProcessReminderNotificationsUseCase"/> so due reminders become notifications without needing an external scheduler.</summary>
public sealed class ReminderNotificationBackgroundService(IServiceScopeFactory scopeFactory, ILogger<ReminderNotificationBackgroundService> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                // BackgroundService is a singleton, but the use case's dependencies (DbContext, repositories)
                // are scoped; create a fresh DI scope for each tick instead of resolving once at startup.
                using var scope = scopeFactory.CreateScope();
                var processor = scope.ServiceProvider.GetRequiredService<IProcessReminderNotificationsUseCase>();
                var created = await processor.ExecuteAsync(DateTime.UtcNow, stoppingToken);
                if (created > 0) logger.LogInformation("Created {NotificationCount} reminder notifications.", created);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                // Never let one failed tick crash the whole background service; log and retry on the next interval.
                logger.LogError(exception, "Reminder notification processing failed.");
            }
        }
    }
}
