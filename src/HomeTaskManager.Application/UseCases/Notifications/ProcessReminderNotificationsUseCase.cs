using HomeTaskManager.Application.Abstractions;
using HomeTaskManager.Application.Abstractions.Repositories;
using HomeTaskManager.Domain.Entities;
using HomeTaskManager.Domain.Enums;
using System.Globalization;

namespace HomeTaskManager.Application.UseCases.Notifications;

public interface IProcessReminderNotificationsUseCase
{
    Task<int> ExecuteAsync(DateTime utcNow, CancellationToken cancellationToken = default);
}

public sealed class ProcessReminderNotificationsUseCase(
    IReminderRepository reminderRepository,
    ITaskTemplateRepository taskTemplateRepository,
    ITaskInstanceRepository taskInstanceRepository,
    IUserPreferencesRepository preferencesRepository,
    INotificationRepository notificationRepository,
    INotificationService notificationService,
    IUnitOfWork unitOfWork) : IProcessReminderNotificationsUseCase
{
    public async Task<int> ExecuteAsync(DateTime utcNow, CancellationToken cancellationToken = default)
    {
        var reminders = await reminderRepository.GetEnabledAsync(cancellationToken);
        var instances = await taskInstanceRepository.GetAssignedSinceAsync(DateOnly.FromDateTime(utcNow).AddDays(-7), cancellationToken);
        var templates = await taskTemplateRepository.GetByIdsAsync(reminders.Select(reminder => reminder.TaskTemplateId).Distinct(), cancellationToken);
        var templatesById = templates.ToDictionary(template => template.Id);
        var created = 0;

        foreach (var reminder in reminders)
        {
            if (!templatesById.TryGetValue(reminder.TaskTemplateId, out var template)) continue;
            foreach (var instance in instances.Where(instance => instance.TaskTemplateId == reminder.TaskTemplateId && instance.AssignedUserId.HasValue && !instance.IsCompleted && instance.Status != TaskInstanceStatus.Cancelled))
            {
                var assignedUserId = instance.AssignedUserId!.Value;
                var triggerDate = reminder.TriggerType == ReminderTriggerType.EndOfDay ? instance.PlannedDate : instance.DueDate;
                var triggerAt = triggerDate.ToDateTime(TimeOnly.MaxValue).Add(-ToTimeSpan(reminder.OffsetValue, reminder.OffsetUnit));
                if (utcNow < triggerAt) continue;
                var link = $"/week?year={instance.PlannedDate.Year}&week={ISOWeek.GetWeekOfYear(instance.PlannedDate.ToDateTime(TimeOnly.MinValue))}&instanceId={instance.Id}";
                if (await notificationRepository.ExistsByLinkAsync(assignedUserId, link, cancellationToken)) continue;
                var preferences = await preferencesRepository.GetByUserIdAsync(assignedUserId, cancellationToken);
                if (preferences?.NotificationsEnabled == false) continue;
                var weekNumber = ISOWeek.GetWeekOfYear(instance.PlannedDate.ToDateTime(TimeOnly.MinValue));
                var title = $"Rappel : {template.Title} · Semaine {instance.PlannedDate.Year}-S{weekNumber:00}";
                var message = $"Cette tâche arrive à échéance le {instance.DueDate:dd/MM/yyyy}.";
                await notificationRepository.AddAsync(new Notification(assignedUserId, title, message, link), cancellationToken);
                await notificationService.SendReminderNotificationAsync(assignedUserId, title, message, cancellationToken);
                created++;
            }
        }
        if (created > 0) await unitOfWork.SaveChangesAsync(cancellationToken);
        return created;
    }

    private static TimeSpan ToTimeSpan(int value, ReminderOffsetUnit unit) => unit == ReminderOffsetUnit.Day ? TimeSpan.FromDays(value) : TimeSpan.FromHours(value);
}