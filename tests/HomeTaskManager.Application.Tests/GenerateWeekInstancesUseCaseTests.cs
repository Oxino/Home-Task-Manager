using FluentAssertions;
using HomeTaskManager.Application.Abstractions.Repositories;
using HomeTaskManager.Application.UseCases.Weeks;
using HomeTaskManager.Domain.Entities;
using HomeTaskManager.Domain.Enums;
using HomeTaskManager.Domain.ValueObjects;

namespace HomeTaskManager.Application.Tests;

/// <summary>In-memory fake standing in for the EF-backed Week store in tests.</summary>
public sealed class InMemoryWeekRepository : IWeekRepository
{
    private readonly List<Week> _weeks = [];

    public Task<Week?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_weeks.SingleOrDefault(week => week.Id == id));

    public Task<Week?> GetByKeyAsync(WeekKey key, CancellationToken cancellationToken = default) =>
        Task.FromResult(_weeks
            .Where(week => week.Year == key.Year && week.WeekNumber == key.WeekNumber)
            .OrderByDescending(week => week.Status == WeekStatus.Active)
            .ThenByDescending(week => week.SegmentNumber)
            .FirstOrDefault());

    public Task<int> GetNextSegmentNumberAsync(WeekKey key, CancellationToken cancellationToken = default) =>
        Task.FromResult(_weeks
            .Where(week => week.Year == key.Year && week.WeekNumber == key.WeekNumber)
            .Select(week => week.SegmentNumber)
            .DefaultIfEmpty()
            .Max() + 1);

    public Task AddAsync(Week week, CancellationToken cancellationToken = default)
    {
        _weeks.Add(week);
        return Task.CompletedTask;
    }
}

/// <summary>In-memory fake standing in for the EF-backed TaskInstance store in tests.</summary>
public sealed class InMemoryTaskInstanceRepository : ITaskInstanceRepository
{
    private readonly List<TaskInstance> _instances = [];

    public Task<TaskInstance?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_instances.SingleOrDefault(instance => instance.Id == id));

    public Task<IReadOnlyList<TaskInstance>> GetByWeekAsync(Guid weekId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<TaskInstance>>(_instances.Where(instance => instance.WeekId == weekId).ToList());

    public Task<bool> ExistsForTaskTemplateAsync(Guid taskTemplateId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_instances.Any(instance => instance.TaskTemplateId == taskTemplateId));

    public Task<IReadOnlyList<TaskInstance>> GetAssignedSinceAsync(DateOnly since, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<TaskInstance>>(_instances
            .Where(instance => instance.AssignedUserId is not null && instance.PlannedDate >= since)
            .ToList());

    public Task AddRangeAsync(IEnumerable<TaskInstance> instances, CancellationToken cancellationToken = default)
    {
        _instances.AddRange(instances);
        return Task.CompletedTask;
    }

    public void RemoveRange(IEnumerable<TaskInstance> instances) => _instances.RemoveAll(instance => instances.Contains(instance));
}

/// <summary>Verifies GenerateWeekInstancesUseCase's per-FrequencyType date computation and its idempotency on re-runs.</summary>
public class GenerateWeekInstancesUseCaseTests
{
    private static TaskTemplate CreateTemplate(FrequencyType frequencyType) =>
        new($"Task-{frequencyType}", Guid.NewGuid(), 40, frequencyType);

    [Fact]
    public async Task DailyMandatory_task_generates_seven_instances()
    {
        var taskTemplateRepository = new InMemoryTaskTemplateRepository();
        taskTemplateRepository.Seed(CreateTemplate(FrequencyType.DailyMandatory));
        var useCase = new GenerateWeekInstancesUseCase(
            new InMemoryWeekRepository(), taskTemplateRepository, new InMemoryTaskInstanceRepository(), new NoOpUnitOfWork());

        var result = await useCase.ExecuteAsync(new GenerateWeekInstancesCommand(2026, 1));

        result.CreatedInstances.Should().HaveCount(7);
        result.CreatedInstances.Select(i => i.PlannedDate).Distinct().Should().HaveCount(7);
    }

    [Fact]
    public async Task DailyMandatory_task_generates_only_the_days_in_an_active_remaining_segment()
    {
        var taskTemplateRepository = new InMemoryTaskTemplateRepository();
        taskTemplateRepository.Seed(CreateTemplate(FrequencyType.DailyMandatory));
        var weekRepository = new InMemoryWeekRepository();
        await weekRepository.AddAsync(new Week(new WeekKey(2026, 2)));
        await weekRepository.AddAsync(new Week(new WeekKey(2026, 2), 2, new DateOnly(2026, 1, 9), new DateOnly(2026, 1, 11)));
        var useCase = new GenerateWeekInstancesUseCase(
            weekRepository, taskTemplateRepository, new InMemoryTaskInstanceRepository(), new NoOpUnitOfWork());

        var result = await useCase.ExecuteAsync(new GenerateWeekInstancesCommand(2026, 2));

        result.Week.SegmentNumber.Should().Be(2);
        result.CreatedInstances.Should().HaveCount(3);
        result.CreatedInstances.Select(instance => instance.PlannedDate.DayOfWeek)
            .Should().BeEquivalentTo([DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday]);
    }

    [Theory]
    [InlineData(FrequencyType.DailyFlexible)]
    [InlineData(FrequencyType.Weekly)]
    public async Task Weekly_style_tasks_generate_a_single_instance(FrequencyType frequencyType)
    {
        var taskTemplateRepository = new InMemoryTaskTemplateRepository();
        taskTemplateRepository.Seed(CreateTemplate(frequencyType));
        var useCase = new GenerateWeekInstancesUseCase(
            new InMemoryWeekRepository(), taskTemplateRepository, new InMemoryTaskInstanceRepository(), new NoOpUnitOfWork());

        var result = await useCase.ExecuteAsync(new GenerateWeekInstancesCommand(2026, 1));

        result.CreatedInstances.Should().ContainSingle();
    }

    [Theory]
    [InlineData(WeeklyScheduleType.StartOfWeek, 0)]
    [InlineData(WeeklyScheduleType.MiddleOfWeek, 3)]
    [InlineData(WeeklyScheduleType.EndOfWeek, 6)]
    public async Task Weekly_task_uses_the_configured_deadline(WeeklyScheduleType scheduleType, int expectedOffset)
    {
        var taskTemplateRepository = new InMemoryTaskTemplateRepository();
        taskTemplateRepository.Seed(new TaskTemplate("Task-WeeklySchedule", Guid.NewGuid(), 40, FrequencyType.Weekly, weeklyScheduleType: scheduleType));
        var useCase = new GenerateWeekInstancesUseCase(
            new InMemoryWeekRepository(), taskTemplateRepository, new InMemoryTaskInstanceRepository(), new NoOpUnitOfWork());

        var result = await useCase.ExecuteAsync(new GenerateWeekInstancesCommand(2026, 1));

        result.CreatedInstances.Single().DueDate.Should().Be(new WeekKey(2026, 1).StartDate.AddDays(expectedOffset));
    }

    [Fact]
    public async Task Weekly_fluid_task_generates_one_instance_per_active_day()
    {
        var taskTemplateRepository = new InMemoryTaskTemplateRepository();
        var template = new TaskTemplate("Task-WeeklyFluid", Guid.NewGuid(), 40, FrequencyType.WeeklyFlexible, activeWeekDays: [DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Saturday]);
        taskTemplateRepository.Seed(template);
        var useCase = new GenerateWeekInstancesUseCase(
            new InMemoryWeekRepository(), taskTemplateRepository, new InMemoryTaskInstanceRepository(), new NoOpUnitOfWork());

        var result = await useCase.ExecuteAsync(new GenerateWeekInstancesCommand(2026, 1));

        result.CreatedInstances.Should().HaveCount(3);
        result.CreatedInstances.Select(instance => instance.PlannedDate.DayOfWeek)
            .Should().BeEquivalentTo([DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Saturday]);
    }

    [Fact]
    public async Task Monthly_task_only_generates_on_the_first_week_of_the_month()
    {
        var taskTemplateRepository = new InMemoryTaskTemplateRepository();
        taskTemplateRepository.Seed(CreateTemplate(FrequencyType.Monthly));
        var weekRepository = new InMemoryWeekRepository();
        var instanceRepository = new InMemoryTaskInstanceRepository();
        var useCase = new GenerateWeekInstancesUseCase(weekRepository, taskTemplateRepository, instanceRepository, new NoOpUnitOfWork());

        var firstWeekOfMonth = Enumerable.Range(1, 12).Select(week => new WeekKey(2026, week))
            .First(key => key.StartDate <= new DateOnly(2026, 1, 1) && key.StartDate.AddDays(6) >= new DateOnly(2026, 1, 1));
        var laterWeekSameMonth = new WeekKey(2026, firstWeekOfMonth.WeekNumber + 1);

        var resultOnFirstWeek = await useCase.ExecuteAsync(
            new GenerateWeekInstancesCommand(firstWeekOfMonth.Year, firstWeekOfMonth.WeekNumber));
        var resultOnNextWeek = await useCase.ExecuteAsync(
            new GenerateWeekInstancesCommand(laterWeekSameMonth.Year, laterWeekSameMonth.WeekNumber));

        resultOnFirstWeek.CreatedInstances.Should().ContainSingle();
        resultOnNextWeek.CreatedInstances.Should().BeEmpty();
    }

    [Fact]
    public async Task Monthly_specific_day_uses_the_last_day_in_short_months()
    {
        var taskTemplateRepository = new InMemoryTaskTemplateRepository();
        var template = new TaskTemplate("Task-MonthlySpecific", Guid.NewGuid(), 40, FrequencyType.Monthly, monthlyScheduleType: MonthlyScheduleType.SpecificDay, monthlyDay: 31);
        taskTemplateRepository.Seed(template);
        var weekRepository = new InMemoryWeekRepository();
        var instanceRepository = new InMemoryTaskInstanceRepository();
        var useCase = new GenerateWeekInstancesUseCase(weekRepository, taskTemplateRepository, instanceRepository, new NoOpUnitOfWork());

        var result = await useCase.ExecuteAsync(new GenerateWeekInstancesCommand(2026, 9));

        result.CreatedInstances.Should().ContainSingle();
        result.CreatedInstances.Single().PlannedDate.Should().Be(new DateOnly(2026, 2, 28));
        result.CreatedInstances.Single().DueDate.Should().Be(new DateOnly(2026, 2, 28));
    }

    [Fact]
    public async Task OneTime_task_generates_only_once_across_weeks()
    {
        var taskTemplateRepository = new InMemoryTaskTemplateRepository();
        taskTemplateRepository.Seed(CreateTemplate(FrequencyType.OneTime));
        var weekRepository = new InMemoryWeekRepository();
        var instanceRepository = new InMemoryTaskInstanceRepository();
        var useCase = new GenerateWeekInstancesUseCase(weekRepository, taskTemplateRepository, instanceRepository, new NoOpUnitOfWork());

        var firstRun = await useCase.ExecuteAsync(new GenerateWeekInstancesCommand(2026, 1));
        var secondRun = await useCase.ExecuteAsync(new GenerateWeekInstancesCommand(2026, 2));

        firstRun.CreatedInstances.Should().ContainSingle();
        secondRun.CreatedInstances.Should().BeEmpty();
    }

    [Fact]
    public async Task Regenerating_the_same_week_is_idempotent()
    {
        var taskTemplateRepository = new InMemoryTaskTemplateRepository();
        taskTemplateRepository.Seed(CreateTemplate(FrequencyType.Weekly));
        var weekRepository = new InMemoryWeekRepository();
        var instanceRepository = new InMemoryTaskInstanceRepository();
        var useCase = new GenerateWeekInstancesUseCase(weekRepository, taskTemplateRepository, instanceRepository, new NoOpUnitOfWork());

        await useCase.ExecuteAsync(new GenerateWeekInstancesCommand(2026, 1));
        var secondRun = await useCase.ExecuteAsync(new GenerateWeekInstancesCommand(2026, 1));

        secondRun.CreatedInstances.Should().BeEmpty();
    }

    [Fact]
    public async Task Generated_instances_start_unassigned()
    {
        var taskTemplateRepository = new InMemoryTaskTemplateRepository();
        taskTemplateRepository.Seed(CreateTemplate(FrequencyType.Weekly));
        var useCase = new GenerateWeekInstancesUseCase(
            new InMemoryWeekRepository(), taskTemplateRepository, new InMemoryTaskInstanceRepository(), new NoOpUnitOfWork());

        var result = await useCase.ExecuteAsync(new GenerateWeekInstancesCommand(2026, 1));

        result.CreatedInstances.Single().AssignedUserId.Should().BeNull();
    }
}
