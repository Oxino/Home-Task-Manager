using FluentAssertions;
using HomeTaskManager.Application.Exceptions;
using HomeTaskManager.Application.UseCases.TaskTemplates;
using HomeTaskManager.Domain.Entities;
using HomeTaskManager.Domain.Enums;

namespace HomeTaskManager.Application.Tests;

/// <summary>Verifies SetTaskTemplateActiveUseCase's admin-only authorization.</summary>
public sealed class SetTaskTemplateActiveUseCaseTests
{
    [Fact]
    public async Task Admin_can_deactivate_and_reactivate_a_task()
    {
        var admin = new User("Admin", isAdmin: true);
        var users = new InMemoryUserRepository();
        users.Seed(admin);
        var task = new TaskTemplate("Aspirateur", admin.Id, 40, FrequencyType.Weekly);
        var tasks = new InMemoryTaskTemplateRepository();
        tasks.Seed(task);
        var useCase = new SetTaskTemplateActiveUseCase(tasks, users, new NoOpAuditLogger(), new NoOpUnitOfWork());

        var inactive = await useCase.ExecuteAsync(new SetTaskTemplateActiveCommand(task.Id, admin.Id, false));
        var active = await useCase.ExecuteAsync(new SetTaskTemplateActiveCommand(task.Id, admin.Id, true));

        inactive.IsActive.Should().BeFalse();
        active.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task Participant_cannot_change_task_activity()
    {
        var participant = new User("Julie");
        var users = new InMemoryUserRepository();
        users.Seed(participant);
        var task = new TaskTemplate("Aspirateur", participant.Id, 40, FrequencyType.Weekly);
        var tasks = new InMemoryTaskTemplateRepository();
        tasks.Seed(task);
        var useCase = new SetTaskTemplateActiveUseCase(tasks, users, new NoOpAuditLogger(), new NoOpUnitOfWork());

        var action = () => useCase.ExecuteAsync(new SetTaskTemplateActiveCommand(task.Id, participant.Id, false));

        await action.Should().ThrowAsync<ForbiddenOperationException>();
    }
}
