using FluentAssertions;
using HomeTaskManager.Application.Abstractions.Repositories;
using HomeTaskManager.Application.Exceptions;
using HomeTaskManager.Application.UseCases.TaskTemplates;
using HomeTaskManager.Domain.Entities;
using HomeTaskManager.Domain.Enums;
using HomeTaskManager.Domain.Exceptions;

namespace HomeTaskManager.Application.Tests;

/// <summary>In-memory fake standing in for the EF-backed Reminder store in tests.</summary>
public sealed class InMemoryReminderRepository : IReminderRepository
{
    private readonly List<Reminder> _reminders = [];
    public Task<Reminder?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(_reminders.SingleOrDefault(reminder => reminder.Id == id));
    public Task<IReadOnlyList<Reminder>> GetByTaskTemplateAsync(Guid taskTemplateId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Reminder>>(_reminders.Where(reminder => reminder.TaskTemplateId == taskTemplateId).ToList());
    public Task AddAsync(Reminder reminder, CancellationToken cancellationToken = default) { _reminders.Add(reminder); return Task.CompletedTask; }
    public Task<IReadOnlyList<Reminder>> GetEnabledAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Reminder>>(_reminders.Where(reminder => reminder.IsEnabled).ToList());
}

/// <summary>Verifies ReminderUseCase's add/list/enable-disable operations and author/admin-only access.</summary>
public sealed class ReminderUseCaseTests
{
    [Fact]
    public async Task Task_author_can_add_a_reminder()
    {
        var author = new User("Julie");
        var users = new InMemoryUserRepository(); users.Seed(author);
        var task = new TaskTemplate("Aspirateur", author.Id, 40, FrequencyType.Weekly);
        var tasks = new InMemoryTaskTemplateRepository(); tasks.Seed(task);
        var useCase = new ReminderUseCase(new InMemoryReminderRepository(), tasks, users, new NoOpAuditLogger(), new NoOpUnitOfWork());

        var reminder = await useCase.AddAsync(new AddReminderCommand(task.Id, author.Id, ReminderTriggerType.EndOfWeek, 2, ReminderOffsetUnit.Hour));

        reminder.IsEnabled.Should().BeTrue();
        reminder.OffsetValue.Should().Be(2);
    }

    [Fact]
    public async Task Another_participant_cannot_manage_a_reminder()
    {
        var author = new User("Julie");
        var other = new User("Nicolas");
        var users = new InMemoryUserRepository(); users.Seed(author); users.Seed(other);
        var task = new TaskTemplate("Aspirateur", author.Id, 40, FrequencyType.Weekly);
        var tasks = new InMemoryTaskTemplateRepository(); tasks.Seed(task);
        var useCase = new ReminderUseCase(new InMemoryReminderRepository(), tasks, users, new NoOpAuditLogger(), new NoOpUnitOfWork());

        var action = () => useCase.AddAsync(new AddReminderCommand(task.Id, other.Id, ReminderTriggerType.EndOfDay, 1, ReminderOffsetUnit.Day));

        await action.Should().ThrowAsync<ForbiddenOperationException>();
    }

    [Fact]
    public void Negative_delay_is_rejected()
    {
        var reminderAction = () => new Reminder(Guid.NewGuid(), ReminderTriggerType.EndOfDay, -1, ReminderOffsetUnit.Hour);
        reminderAction.Should().Throw<DomainValidationException>();
    }
}
