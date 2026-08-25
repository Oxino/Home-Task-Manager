using FluentAssertions;
using HomeTaskManager.Application.Abstractions.Repositories;
using HomeTaskManager.Application.Exceptions;
using HomeTaskManager.Application.UseCases.TaskInstances;
using HomeTaskManager.Domain.Entities;
using HomeTaskManager.Domain.Exceptions;
using HomeTaskManager.Domain.ValueObjects;

namespace HomeTaskManager.Application.Tests;

public sealed class InMemoryTaskCommentRepository : ITaskCommentRepository
{
    private readonly List<TaskComment> _comments = [];

    public Task<IReadOnlyList<TaskComment>> GetByTaskInstanceAsync(Guid taskInstanceId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<TaskComment>>(_comments.Where(comment => comment.TaskInstanceId == taskInstanceId).ToList());

    public Task AddAsync(TaskComment comment, CancellationToken cancellationToken = default)
    {
        _comments.Add(comment);
        return Task.CompletedTask;
    }
}

public sealed class TaskCommentUseCaseTests
{
    [Fact]
    public async Task Assigned_participant_can_add_and_read_a_comment()
    {
        var owner = new User("Julie");
        var users = new InMemoryUserRepository();
        users.Seed(owner);
        var week = new Week(new WeekKey(2026, 1));
        var weeks = new InMemoryWeekRepository();
        await weeks.AddAsync(week);
        var instance = new TaskInstance(Guid.NewGuid(), Guid.NewGuid(), week, owner.Id, week.StartDate, week.EndDate);
        var instances = new InMemoryTaskInstanceRepository();
        await instances.AddRangeAsync([instance]);
        var useCase = new TaskCommentUseCase(new InMemoryTaskCommentRepository(), instances, users, new NoOpAuditLogger(), new NoOpUnitOfWork());

        var created = await useCase.AddAsync(new AddTaskCommentCommand(instance.Id, owner.Id, "Fait après le déjeuner."));
        var comments = await useCase.ListAsync(instance.Id, owner.Id);

        created.Content.Should().Be("Fait après le déjeuner.");
        comments.Should().ContainSingle();
    }

    [Fact]
    public async Task Another_participant_cannot_access_task_comments()
    {
        var owner = new User("Julie");
        var other = new User("Nicolas");
        var users = new InMemoryUserRepository();
        users.Seed(owner);
        users.Seed(other);
        var week = new Week(new WeekKey(2026, 1));
        var weeks = new InMemoryWeekRepository();
        await weeks.AddAsync(week);
        var instance = new TaskInstance(Guid.NewGuid(), Guid.NewGuid(), week, owner.Id, week.StartDate, week.EndDate);
        var instances = new InMemoryTaskInstanceRepository();
        await instances.AddRangeAsync([instance]);
        var useCase = new TaskCommentUseCase(new InMemoryTaskCommentRepository(), instances, users, new NoOpAuditLogger(), new NoOpUnitOfWork());

        var action = () => useCase.ListAsync(instance.Id, other.Id);

        await action.Should().ThrowAsync<ForbiddenOperationException>();
    }

    [Fact]
    public async Task Empty_comment_is_rejected()
    {
        var owner = new User("Julie");
        var users = new InMemoryUserRepository();
        users.Seed(owner);
        var week = new Week(new WeekKey(2026, 1));
        var weeks = new InMemoryWeekRepository();
        await weeks.AddAsync(week);
        var instance = new TaskInstance(Guid.NewGuid(), Guid.NewGuid(), week, owner.Id, week.StartDate, week.EndDate);
        var instances = new InMemoryTaskInstanceRepository();
        await instances.AddRangeAsync([instance]);
        var useCase = new TaskCommentUseCase(new InMemoryTaskCommentRepository(), instances, users, new NoOpAuditLogger(), new NoOpUnitOfWork());

        var action = () => useCase.AddAsync(new AddTaskCommentCommand(instance.Id, owner.Id, "  "));

        await action.Should().ThrowAsync<DomainValidationException>();
    }
}
