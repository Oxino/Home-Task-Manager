using FluentAssertions;
using HomeTaskManager.Application.Exceptions;
using HomeTaskManager.Application.UseCases.TaskInstances;
using HomeTaskManager.Domain.Entities;
using HomeTaskManager.Domain.Exceptions;
using HomeTaskManager.Domain.ValueObjects;

namespace HomeTaskManager.Application.Tests;

public class SetTaskInstanceCompletionUseCaseTests
{
    private static SetTaskInstanceCompletionUseCase CreateUseCase(
        InMemoryTaskInstanceRepository instanceRepository,
        InMemoryWeekRepository weekRepository,
        InMemoryUserRepository userRepository) =>
        new(instanceRepository, weekRepository, userRepository, new NoOpAuditLogger(), new NoOpUnitOfWork());

    [Fact]
    public async Task Owner_can_mark_their_task_as_completed()
    {
        var userRepository = new InMemoryUserRepository();
        var owner = new User("Julie");
        userRepository.Seed(owner);
        var weekRepository = new InMemoryWeekRepository();
        var week = new Week(new WeekKey(2026, 1));
        await weekRepository.AddAsync(week);
        var instance = new TaskInstance(Guid.NewGuid(), Guid.NewGuid(), week, owner.Id, week.StartDate, week.EndDate);
        var instanceRepository = new InMemoryTaskInstanceRepository();
        await instanceRepository.AddRangeAsync([instance]);
        var useCase = CreateUseCase(instanceRepository, weekRepository, userRepository);

        var result = await useCase.ExecuteAsync(new SetTaskInstanceCompletionCommand(instance.Id, owner.Id, true));

        result.IsCompleted.Should().BeTrue();
    }

    [Fact]
    public async Task Another_user_cannot_change_someone_elses_completion()
    {
        var userRepository = new InMemoryUserRepository();
        var owner = new User("Julie");
        var otherUser = new User("Nicolas");
        userRepository.Seed(owner);
        userRepository.Seed(otherUser);
        var weekRepository = new InMemoryWeekRepository();
        var week = new Week(new WeekKey(2026, 1));
        await weekRepository.AddAsync(week);
        var instance = new TaskInstance(Guid.NewGuid(), Guid.NewGuid(), week, owner.Id, week.StartDate, week.EndDate);
        var instanceRepository = new InMemoryTaskInstanceRepository();
        await instanceRepository.AddRangeAsync([instance]);
        var useCase = CreateUseCase(instanceRepository, weekRepository, userRepository);

        var action = () => useCase.ExecuteAsync(new SetTaskInstanceCompletionCommand(instance.Id, otherUser.Id, true));

        await action.Should().ThrowAsync<ForbiddenOperationException>();
    }

    [Fact]
    public async Task Completion_is_locked_once_the_week_is_closed()
    {
        var userRepository = new InMemoryUserRepository();
        var owner = new User("Julie");
        userRepository.Seed(owner);
        var weekRepository = new InMemoryWeekRepository();
        var week = new Week(new WeekKey(2026, 1));
        week.Close();
        await weekRepository.AddAsync(week);
        var instance = new TaskInstance(Guid.NewGuid(), Guid.NewGuid(), week, owner.Id, week.StartDate, week.EndDate);
        var instanceRepository = new InMemoryTaskInstanceRepository();
        await instanceRepository.AddRangeAsync([instance]);
        var useCase = CreateUseCase(instanceRepository, weekRepository, userRepository);

        var action = () => useCase.ExecuteAsync(new SetTaskInstanceCompletionCommand(instance.Id, owner.Id, true));

        await action.Should().ThrowAsync<DomainValidationException>();
    }

    [Fact]
    public async Task Rejects_an_unknown_task_instance()
    {
        var userRepository = new InMemoryUserRepository();
        var owner = new User("Julie");
        userRepository.Seed(owner);
        var useCase = CreateUseCase(new InMemoryTaskInstanceRepository(), new InMemoryWeekRepository(), userRepository);

        var action = () => useCase.ExecuteAsync(new SetTaskInstanceCompletionCommand(Guid.NewGuid(), owner.Id, true));

        await action.Should().ThrowAsync<DomainValidationException>();
    }
}
