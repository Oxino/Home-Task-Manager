using FluentAssertions;
using HomeTaskManager.Application.Exceptions;
using HomeTaskManager.Application.UseCases.TaskInstances;
using HomeTaskManager.Domain.Entities;
using HomeTaskManager.Domain.Exceptions;
using HomeTaskManager.Domain.ValueObjects;

namespace HomeTaskManager.Application.Tests;

/// <summary>Verifies CancelTaskInstanceUseCase's admin-only authorization and closed-week guard.</summary>
public sealed class CancelTaskInstanceUseCaseTests
{
    [Fact]
    public async Task Admin_can_cancel_an_open_instance()
    {
        var admin = new User("Admin", isAdmin: true);
        var users = new InMemoryUserRepository();
        users.Seed(admin);
        var week = new Week(new WeekKey(2026, 1));
        var weeks = new InMemoryWeekRepository();
        await weeks.AddAsync(week);
        var instance = new TaskInstance(Guid.NewGuid(), Guid.NewGuid(), week, admin.Id, week.StartDate, week.EndDate);
        var instances = new InMemoryTaskInstanceRepository();
        await instances.AddRangeAsync([instance]);
        var useCase = new CancelTaskInstanceUseCase(instances, weeks, users, new NoOpAuditLogger(), new NoOpUnitOfWork());

        var result = await useCase.ExecuteAsync(new CancelTaskInstanceCommand(instance.Id, admin.Id));

        result.Status.Should().Be("Cancelled");
    }

    [Fact]
    public async Task A_completed_instance_cannot_be_cancelled()
    {
        var admin = new User("Admin", isAdmin: true);
        var users = new InMemoryUserRepository();
        users.Seed(admin);
        var week = new Week(new WeekKey(2026, 1));
        var weeks = new InMemoryWeekRepository();
        await weeks.AddAsync(week);
        var instance = new TaskInstance(Guid.NewGuid(), Guid.NewGuid(), week, admin.Id, week.StartDate, week.EndDate);
        instance.SetCompletion(true, week);
        var instances = new InMemoryTaskInstanceRepository();
        await instances.AddRangeAsync([instance]);
        var useCase = new CancelTaskInstanceUseCase(instances, weeks, users, new NoOpAuditLogger(), new NoOpUnitOfWork());

        var action = () => useCase.ExecuteAsync(new CancelTaskInstanceCommand(instance.Id, admin.Id));

        await action.Should().ThrowAsync<DomainValidationException>();
    }
}
