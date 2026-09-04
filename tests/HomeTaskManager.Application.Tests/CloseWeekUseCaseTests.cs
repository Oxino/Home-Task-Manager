using FluentAssertions;
using HomeTaskManager.Application.Exceptions;
using HomeTaskManager.Application.UseCases.Weeks;
using HomeTaskManager.Domain.Entities;
using HomeTaskManager.Domain.Exceptions;
using HomeTaskManager.Domain.ValueObjects;

namespace HomeTaskManager.Application.Tests;

/// <summary>Verifies CloseWeekUseCase's carry-over logic (ADR-005), early-closure segmentation, and cancel-incomplete option.</summary>
public class CloseWeekUseCaseTests
{
    private static CloseWeekUseCase CreateUseCase(
        InMemoryWeekRepository weekRepository,
        InMemoryUserRepository userRepository,
        InMemoryTaskInstanceRepository instanceRepository) =>
        new(weekRepository, userRepository, instanceRepository, new InMemoryTaskTemplateRepository(), new NoOpAuditLogger(), new NoOpUnitOfWork());

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
        var week = new Week(new WeekKey(2026, 2));
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

    [Fact]
    public async Task Already_carried_over_unfinished_instances_are_not_carried_over_again()
    {
        var userRepository = new InMemoryUserRepository();
        var admin = new User("Nicolas", isAdmin: true);
        var owner = new User("Julie");
        userRepository.Seed(admin);
        userRepository.Seed(owner);
        var weekRepository = new InMemoryWeekRepository();
        var week = new Week(new WeekKey(2026, 2));
        await weekRepository.AddAsync(week);
        var instanceRepository = new InMemoryTaskInstanceRepository();
        var carriedOver = new TaskInstance(Guid.NewGuid(), null, week, owner.Id, week.StartDate, week.StartDate, isCarryOver: true, parentTaskInstanceId: Guid.NewGuid());
        await instanceRepository.AddRangeAsync([carriedOver]);
        var useCase = CreateUseCase(weekRepository, userRepository, instanceRepository);

        var result = await useCase.ExecuteAsync(new CloseWeekCommand(2026, 2, admin.Id));

        result.CarriedOverInstances.Should().BeEmpty();
    }

    [Fact]
    public async Task Early_closure_creates_a_remaining_segment_and_carries_open_instances_into_it()
    {
        var userRepository = new InMemoryUserRepository();
        var admin = new User("Nicolas", isAdmin: true);
        var owner = new User("Julie");
        userRepository.Seed(admin);
        userRepository.Seed(owner);
        var week = new Week(new WeekKey(2026, 2));
        var weekRepository = new InMemoryWeekRepository();
        await weekRepository.AddAsync(week);
        var instanceRepository = new InMemoryTaskInstanceRepository();
        var unfinished = new TaskInstance(Guid.NewGuid(), Guid.NewGuid(), week, owner.Id, week.StartDate, week.EndDate);
        await instanceRepository.AddRangeAsync([unfinished]);
        var useCase = CreateUseCase(weekRepository, userRepository, instanceRepository);

        var closedAt = new DateTime(2026, 1, 8, 14, 30, 0, DateTimeKind.Utc);
        var result = await useCase.ExecuteAsync(new CloseWeekCommand(2026, 2, admin.Id, ClosedAt: closedAt));

        result.Week.Status.Should().Be(nameof(Domain.Enums.WeekStatus.Closed));
        result.NextPeriod.Should().NotBeNull();
        var continuation = result.NextPeriod!;
        continuation.Year.Should().Be(2026);
        continuation.WeekNumber.Should().Be(2);
        continuation.SegmentNumber.Should().Be(2);
        continuation.StartDate.Should().Be(new DateOnly(2026, 1, 8));
        continuation.EndDate.Should().Be(new DateOnly(2026, 1, 11));
        continuation.StartsAt.Should().Be(closedAt);
        continuation.Status.Should().Be(nameof(Domain.Enums.WeekStatus.Active));
        result.CarriedOverInstances.Should().ContainSingle().Which.WeekId.Should().Be(continuation.Id);
    }

    [Fact]
    public async Task Early_closure_can_cancel_open_instances_and_start_without_history()
    {
        var userRepository = new InMemoryUserRepository();
        var admin = new User("Nicolas", isAdmin: true);
        var owner = new User("Julie");
        userRepository.Seed(admin);
        userRepository.Seed(owner);
        var week = new Week(new WeekKey(2026, 2));
        var weekRepository = new InMemoryWeekRepository();
        await weekRepository.AddAsync(week);
        var instanceRepository = new InMemoryTaskInstanceRepository();
        var unfinished = new TaskInstance(Guid.NewGuid(), Guid.NewGuid(), week, owner.Id, week.StartDate, week.EndDate);
        await instanceRepository.AddRangeAsync([unfinished]);
        var useCase = CreateUseCase(weekRepository, userRepository, instanceRepository);

        var result = await useCase.ExecuteAsync(new CloseWeekCommand(2026, 2, admin.Id, CancelIncompleteTasks: true, ClosedAt: new DateTime(2026, 1, 8, 14, 30, 0, DateTimeKind.Utc)));

        unfinished.Status.Should().Be(Domain.Enums.TaskInstanceStatus.Cancelled);
        result.CarriedOverInstances.Should().BeEmpty();
        result.NextPeriod.Should().NotBeNull();
        result.NextPeriod!.IgnoreHistoryForAssignment.Should().BeTrue();
    }

    [Fact]
    public async Task Recovers_an_early_closed_week_into_an_active_continuation_period()
    {
        var userRepository = new InMemoryUserRepository();
        var admin = new User("Nicolas", isAdmin: true);
        userRepository.Seed(admin);
        var closedAt = new DateTime(2026, 1, 8, 12, 0, 0, DateTimeKind.Utc);
        var closedWeek = new Week(new WeekKey(2026, 2));
        closedWeek.Close(closedAt);
        var weekRepository = new InMemoryWeekRepository();
        await weekRepository.AddAsync(closedWeek);
        var useCase = new RecoverWeekContinuationUseCase(
            weekRepository,
            userRepository,
            new InMemoryTaskInstanceRepository(),
            new NoOpAuditLogger(),
            new NoOpUnitOfWork());

        var continuation = await useCase.ExecuteAsync(new RecoverWeekContinuationCommand(2026, 2, admin.Id));

        continuation.SegmentNumber.Should().Be(2);
        continuation.Status.Should().Be(nameof(Domain.Enums.WeekStatus.Active));
        continuation.StartsAt.Should().Be(closedAt);
        continuation.EndsAt.Should().Be(new DateTime(2026, 1, 11, 23, 59, 59, 999, DateTimeKind.Utc).AddTicks(9999));
    }

    [Fact]
    public async Task Recovery_consolidates_duplicate_template_carry_overs_into_one_instance()
    {
        var userRepository = new InMemoryUserRepository();
        var admin = new User("Nicolas", isAdmin: true);
        var owner = new User("Julie");
        userRepository.Seed(admin);
        userRepository.Seed(owner);
        var closedAt = new DateTime(2026, 1, 8, 12, 0, 0, DateTimeKind.Utc);
        var closedWeek = new Week(new WeekKey(2026, 2));
        closedWeek.Close(closedAt);
        var followingWeek = new Week(new WeekKey(2026, 3));
        var weekRepository = new InMemoryWeekRepository();
        await weekRepository.AddAsync(closedWeek);
        await weekRepository.AddAsync(followingWeek);
        var instanceRepository = new InMemoryTaskInstanceRepository();
        var templateId = Guid.NewGuid();
        var firstOriginal = new TaskInstance(templateId, null, closedWeek, owner.Id, closedWeek.StartDate, closedWeek.StartDate);
        var secondOriginal = new TaskInstance(templateId, null, closedWeek, owner.Id, closedWeek.StartDate.AddDays(1), closedWeek.StartDate.AddDays(1));
        var firstCarryOver = new TaskInstance(templateId, null, followingWeek, owner.Id, followingWeek.StartDate, followingWeek.StartDate, isCarryOver: true, parentTaskInstanceId: firstOriginal.Id);
        var secondCarryOver = new TaskInstance(templateId, null, followingWeek, owner.Id, followingWeek.StartDate.AddDays(1), followingWeek.StartDate.AddDays(1), isCarryOver: true, parentTaskInstanceId: secondOriginal.Id);
        await instanceRepository.AddRangeAsync([firstOriginal, secondOriginal, firstCarryOver, secondCarryOver]);
        var useCase = new RecoverWeekContinuationUseCase(weekRepository, userRepository, instanceRepository, new NoOpAuditLogger(), new NoOpUnitOfWork());

        var continuation = await useCase.ExecuteAsync(new RecoverWeekContinuationCommand(2026, 2, admin.Id));
        var recoveredInstances = await instanceRepository.GetByWeekAsync(continuation.Id);

        recoveredInstances.Should().ContainSingle();
        recoveredInstances.Single().PlannedDate.Should().Be(continuation.StartDate);
        firstCarryOver.Status.Should().Be(Domain.Enums.TaskInstanceStatus.Cancelled);
        secondCarryOver.Status.Should().Be(Domain.Enums.TaskInstanceStatus.Cancelled);
    }
}
