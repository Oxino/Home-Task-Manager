using HomeTaskManager.Application.Abstractions;
using HomeTaskManager.Application.Abstractions.Repositories;
using HomeTaskManager.Domain.Entities;
using HomeTaskManager.Domain.Enums;
using System.Globalization;

namespace HomeTaskManager.Application.UseCases.Notifications;

/// <summary>Scans enabled <see cref="Reminder"/>s and creates due in-app notifications; invoked periodically by <c>ReminderNotificationBackgroundService</c>.</summary>
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
    /// <summary>Returns the number of notifications created this pass; safe to call repeatedly (idempotent via the link-existence check below).</summary>
    public async Task<int> ExecuteAsync(DateTime utcNow, CancellationToken cancellationToken = default)
    {
        var reminders = await reminderRepository.GetEnabledAsync(cancellationToken);
        // Only look at recent instances (last 7 days) since older ones can no longer have a pending, not-yet-fired reminder.
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
                // The reminder fires OffsetValue/OffsetUnit before its trigger anchor (end of the planned day or the due date).
                var triggerDate = reminder.TriggerType == ReminderTriggerType.EndOfDay ? instance.PlannedDate : instance.DueDate;
                var triggerAt = triggerDate.ToDateTime(TimeOnly.MaxValue).Add(-ToTimeSpan(reminder.OffsetValue, reminder.OffsetUnit));
                if (utcNow < triggerAt) continue;
                var link = $"/week?year={instance.PlannedDate.Year}&week={ISOWeek.GetWeekOfYear(instance.PlannedDate.ToDateTime(TimeOnly.MinValue))}&instanceId={instance.Id}";
                // Idempotency guard: this exact link would already exist for this user if the reminder already fired in a previous pass.
                if (await notificationRepository.ExistsByLinkAsync(assignedUserId, link, cancellationToken)) continue;
                var preferences = await preferencesRepository.GetByUserIdAsync(assignedUserId, cancellationToken);
                if (preferences?.NotificationPreference is "None" or "Important") continue;
                var weekNumber = ISOWeek.GetWeekOfYear(instance.PlannedDate.ToDateTime(TimeOnly.MinValue));
                var title = $"Rappel : {template.Title} · Semaine {instance.PlannedDate.Year}-S{weekNumber:00}";
                var message = $"Cette tâche arrive à échéance le {instance.DueDate:dd/MM/yyyy}.";
                await notificationRepository.AddAsync(new Notification(assignedUserId, title, message, link, NotificationImportance.Normal), cancellationToken);
                await notificationService.SendNotificationAsync(assignedUserId, title, message, NotificationImportance.Normal, cancellationToken);
                created++;
            }
        }
        if (created > 0) await unitOfWork.SaveChangesAsync(cancellationToken);
        return created;
    }

    private static TimeSpan ToTimeSpan(int value, ReminderOffsetUnit unit) => unit == ReminderOffsetUnit.Day ? TimeSpan.FromDays(value) : TimeSpan.FromHours(value);
}