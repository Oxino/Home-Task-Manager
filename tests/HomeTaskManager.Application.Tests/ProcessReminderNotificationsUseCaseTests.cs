using FluentAssertions;
using HomeTaskManager.Application.Abstractions.Repositories;
using HomeTaskManager.Application.UseCases.Notifications;
using HomeTaskManager.Domain.Entities;
using HomeTaskManager.Domain.Enums;
using HomeTaskManager.Domain.ValueObjects;

namespace HomeTaskManager.Application.Tests;

public sealed class ProcessReminderNotificationsUseCaseTests
{
    [Fact]
    public async Task Creates_one_notification_for_a_due_open_task()
    {
        var user = new User("Julie");
        var users = new InMemoryUserRepository(); users.Seed(user);
        var template = new TaskTemplate("Aspirateur", user.Id, 40, FrequencyType.Weekly);
        var templates = new InMemoryTaskTemplateRepository(); templates.Seed(template);
        var week = new Week(new WeekKey(2026, 1)); var weeks = new InMemoryWeekRepository(); await weeks.AddAsync(week);
        var instance = new TaskInstance(template.Id, null, week, user.Id, week.StartDate, week.EndDate); var instances = new InMemoryTaskInstanceRepository(); await instances.AddRangeAsync([instance]);
        var reminders = new InMemoryReminderRepository(); await reminders.AddAsync(new Reminder(template.Id, ReminderTriggerType.EndOfWeek, 0, ReminderOffsetUnit.Hour));
        var notifications = new InMemoryNotificationRepository();
        var preferences = new InMemoryUserPreferencesRepository();
        var notificationService = new InMemoryNotificationService();
        var useCase = new ProcessReminderNotificationsUseCase(reminders, templates, instances, preferences, notifications, notificationService, new NoOpUnitOfWork());

        var created = await useCase.ExecuteAsync(week.EndDate.ToDateTime(TimeOnly.MaxValue));
        var createdAgain = await useCase.ExecuteAsync(week.EndDate.ToDateTime(TimeOnly.MaxValue).AddMinutes(1));

        created.Should().Be(1);
        createdAgain.Should().Be(0);
        var createdNotification = (await notifications.GetByUserIdAsync(user.Id)).Should().ContainSingle().Subject;
        createdNotification.Link.Should().Be($"/week?year={instance.PlannedDate.Year}&week=1&instanceId={instance.Id}");
    }
}
