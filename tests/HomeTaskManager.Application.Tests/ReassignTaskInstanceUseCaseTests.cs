using FluentAssertions;
using HomeTaskManager.Application.Exceptions;
using HomeTaskManager.Application.UseCases.TaskInstances;
using HomeTaskManager.Application.Abstractions.Repositories;
using HomeTaskManager.Domain.Entities;
using HomeTaskManager.Domain.Exceptions;
using HomeTaskManager.Domain.ValueObjects;

namespace HomeTaskManager.Application.Tests;

public sealed class ReassignTaskInstanceUseCaseTests
{
    private static async Task<(User Admin, User Owner, User Target, Week Week, TaskInstance Instance, ReassignTaskInstanceUseCase UseCase, InMemoryNotificationRepository Notifications)> CreateContext(bool closed = false, bool withNotifications = false)
    {
        var admin = new User("Admin", isAdmin: true);
        var owner = new User("Julie");
        var target = new User("Nicolas");
        var users = new InMemoryUserRepository();
        users.Seed(admin);
        users.Seed(owner);
        users.Seed(target);
        var week = new Week(new WeekKey(2026, 1));
        if (closed) week.Close();
        var weeks = new InMemoryWeekRepository();
        await weeks.AddAsync(week);
        var instances = new InMemoryTaskInstanceRepository();
        var notifications = withNotifications ? new InMemoryNotificationRepository() : null;
        var templates = new InMemoryTaskTemplateRepository();
        var template = new TaskTemplate("Sortir les poubelles", admin.Id, 20, Domain.Enums.FrequencyType.Weekly);
        templates.Seed(template);
        var instance = new TaskInstance(template.Id, null, week, owner.Id, week.StartDate, week.EndDate);
        await instances.AddRangeAsync([instance]);
        var useCase = new ReassignTaskInstanceUseCase(instances, weeks, users, new NoOpAuditLogger(), new NoOpUnitOfWork(), notifications, templates);
        return (admin, owner, target, week, instance, useCase, notifications ?? new InMemoryNotificationRepository());
    }

    [Fact]
    public async Task Admin_can_reassign_an_instance_to_an_active_user()
    {
        var context = await CreateContext();
        var result = await context.UseCase.ExecuteAsync(new ReassignTaskInstanceCommand(context.Instance.Id, context.Admin.Id, context.Target.Id));
        result.AssignedUserId.Should().Be(context.Target.Id);
    }

    [Fact]
    public async Task Reassignment_notifies_the_new_owner_with_the_reassigner_name()
    {
        var context = await CreateContext(withNotifications: true);

        await context.UseCase.ExecuteAsync(new ReassignTaskInstanceCommand(context.Instance.Id, context.Admin.Id, context.Target.Id));

        var notification = (await context.Notifications.GetByUserIdAsync(context.Target.Id)).Should().ContainSingle().Subject;
        notification.Title.Should().Be("Tâche réaffectée");
        notification.Message.Should().Be("La tâche Sortir les poubelles vous a été réaffectée par Admin.");
        notification.Link.Should().Be($"/week?year=2026&week=1&instanceId={context.Instance.Id}");
    }

    [Fact]
    public async Task Non_admin_cannot_reassign_an_instance()
    {
        var context = await CreateContext();
        var action = () => context.UseCase.ExecuteAsync(new ReassignTaskInstanceCommand(context.Instance.Id, context.Owner.Id, context.Target.Id));
        await action.Should().ThrowAsync<ForbiddenOperationException>();
    }

    [Fact]
    public async Task Closed_week_cannot_be_reassigned()
    {
        var context = await CreateContext(closed: true);
        var action = () => context.UseCase.ExecuteAsync(new ReassignTaskInstanceCommand(context.Instance.Id, context.Admin.Id, context.Target.Id));
        await action.Should().ThrowAsync<DomainValidationException>();
    }
}
