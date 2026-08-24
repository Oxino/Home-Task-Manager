using FluentAssertions;
using HomeTaskManager.Application.Exceptions;
using HomeTaskManager.Application.UseCases.Weeks;
using HomeTaskManager.Domain.Entities;
using HomeTaskManager.Domain.Exceptions;
using HomeTaskManager.Domain.ValueObjects;

namespace HomeTaskManager.Application.Tests;

public class CloseWeekUseCaseTests
{
    private static CloseWeekUseCase CreateUseCase(
        InMemoryWeekRepository weekRepository,
        InMemoryUserRepository userRepository,
        InMemoryTaskInstanceRepository instanceRepository) =>
        new(weekRepository, userRepository, instanceRepository, new NoOpUnitOfWork());

    [Fact]
    public async Task Closes_the_week_and_carries_over_unfinished_instances()
    {
        var userRepository = new InMemoryUserRepository();
        var admin = new User("Nicolas", isAdmin: true);
        var owner = new User("Julie");
        userRepository.Seed(admin);
        userRepository.Seed(owner);
        var weekRepository = new InMemoryWeekRepository();
        var week = new Week(new WeekKey(2026, 1));
        await weekRepository.AddAsync(week);
        var instanceRepository = new InMemoryTaskInstanceRepository();
        var templateId = Guid.NewGuid();
        var unfinished = new TaskInstance(templateId, Guid.NewGuid(), week, owner.Id, week.StartDate, week.EndDate);
        await instanceRepository.AddRangeAsync([unfinished]);
        var useCase = CreateUseCase(weekRepository, userRepository, instanceRepository);

        var result = await useCase.ExecuteAsync(new CloseWeekCommand(2026, 1, admin.Id));

        result.Week.Status.Should().Be(nameof(Domain.Enums.WeekStatus.Closed));
        var carriedOver = result.CarriedOverInstances.Should().ContainSingle().Subject;
        carriedOver.AssignedUserId.Should().Be(owner.Id);
        carriedOver.IsCarryOver.Should().BeTrue();
    }

    [Fact]
    public async Task Completed_instances_are_not_carried_over()
    {
        var userRepository = new InMemoryUserRepository();
        var admin = new User("Nicolas", isAdmin: true);
        var owner = new User("Julie");
        userRepository.Seed(admin);
        userRepository.Seed(owner);
        var weekRepository = new InMemoryWeekRepository();
        var week = new Week(new WeekKey(2026, 1));
        await weekRepository.AddAsync(week);
        var instanceRepository = new InMemoryTaskInstanceRepository();
        var completed = new TaskInstance(Guid.NewGuid(), Guid.NewGuid(), week, owner.Id, week.StartDate, week.EndDate);
        completed.SetCompletion(true, week);
        await instanceRepository.AddRangeAsync([completed]);
        var useCase = CreateUseCase(weekRepository, userRepository, instanceRepository);

        var result = await useCase.ExecuteAsync(new CloseWeekCommand(2026, 1, admin.Id));

        result.CarriedOverInstances.Should().BeEmpty();
    }

    [Fact]
    public async Task Non_admin_cannot_close_a_week()
    {
        var userRepository = new InMemoryUserRepository();
        var regularUser = new User("Julie");
        userRepository.Seed(regularUser);
        var weekRepository = new InMemoryWeekRepository();
        await weekRepository.AddAsync(new Week(new WeekKey(2026, 1)));
        var useCase = CreateUseCase(weekRepository, userRepository, new InMemoryTaskInstanceRepository());

        var action = () => useCase.ExecuteAsync(new CloseWeekCommand(2026, 1, regularUser.Id));

        await action.Should().ThrowAsync<ForbiddenOperationException>();
    }

    [Fact]
    public async Task Cannot_close_an_already_closed_week()
    {
        var userRepository = new InMemoryUserRepository();
        var admin = new User("Nicolas", isAdmin: true);
        userRepository.Seed(admin);
        var weekRepository = new InMemoryWeekRepository();
        var week = new Week(new WeekKey(2026, 1));
        week.Close();
        await weekRepository.AddAsync(week);
        var useCase = CreateUseCase(weekRepository, userRepository, new InMemoryTaskInstanceRepository());

        var action = () => useCase.ExecuteAsync(new CloseWeekCommand(2026, 1, admin.Id));

        await action.Should().ThrowAsync<DomainValidationException>();
    }

    [Fact]
    public async Task Carry_over_avoids_colliding_with_an_already_generated_next_week_instance()
    {
        var userRepository = new InMemoryUserRepository();
        var admin = new User("Nicolas", isAdmin: true);
        var owner = new User("Julie");
        userRepository.Seed(admin);
        userRepository.Seed(owner);
        var weekRepository = new InMemoryWeekRepository();
        var week = new Week(new WeekKey(2026, 1));
        var nextWeek = new Week(new WeekKey(2026, 1).Next());
        await weekRepository.AddAsync(week);
        await weekRepository.AddAsync(nextWeek);
        var instanceRepository = new InMemoryTaskInstanceRepository();
        var templateId = Guid.NewGuid();
        var unfinished = new TaskInstance(templateId, Guid.NewGuid(), week, owner.Id, week.StartDate, week.EndDate);
        var alreadyGeneratedNextWeekInstance = new TaskInstance(
            templateId, null, nextWeek, null, nextWeek.StartDate, nextWeek.EndDate);
        await instanceRepository.AddRangeAsync([unfinished, alreadyGeneratedNextWeekInstance]);
        var useCase = CreateUseCase(weekRepository, userRepository, instanceRepository);

        var result = await useCase.ExecuteAsync(new CloseWeekCommand(2026, 1, admin.Id));

        var carriedOver = result.CarriedOverInstances.Should().ContainSingle().Subject;
        carriedOver.PlannedDate.Should().Be(nextWeek.StartDate.AddDays(1));
    }
}
