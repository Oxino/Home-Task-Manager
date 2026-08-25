using HomeTaskManager.Application.UseCases.Notifications;

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
                logger.LogError(exception, "Reminder notification processing failed.");
            }
        }
    }
}
