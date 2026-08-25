using FluentAssertions;
using HomeTaskManager.Application.Abstractions.Repositories;
using HomeTaskManager.Application.Exceptions;
using HomeTaskManager.Application.UseCases.TaskInstances;
using HomeTaskManager.Domain.Entities;
using HomeTaskManager.Domain.Enums;
using HomeTaskManager.Domain.Exceptions;
using HomeTaskManager.Domain.ValueObjects;

namespace HomeTaskManager.Application.Tests;

public sealed class InMemoryDailyFlexibleExecutionRepository : IDailyFlexibleExecutionRepository
{
    private readonly List<DailyFlexibleExecution> _executions = [];
    public Task<IReadOnlyList<DailyFlexibleExecution>> GetByTaskInstanceAsync(Guid taskInstanceId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<DailyFlexibleExecution>>(_executions.Where(execution => execution.TaskInstanceId == taskInstanceId).ToList());
    public Task AddAsync(DailyFlexibleExecution execution, CancellationToken cancellationToken = default) { _executions.Add(execution); return Task.CompletedTask; }
}

public sealed class ExecuteDailyFlexibleTaskUseCaseTests
{
    [Fact]
    public async Task First_execution_completes_a_flexible_task()
    {
        var owner = new User("Julie");
        var users = new InMemoryUserRepository(); users.Seed(owner);
        var week = new Week(new WeekKey(2026, 1)); var weeks = new InMemoryWeekRepository(); await weeks.AddAsync(week);
        var template = new TaskTemplate("Plantes", owner.Id, 20, FrequencyType.DailyFlexible);
        var templates = new InMemoryTaskTemplateRepository(); templates.Seed(template);
        var instance = new TaskInstance(template.Id, null, week, owner.Id, week.StartDate, week.EndDate);
        var instances = new InMemoryTaskInstanceRepository(); await instances.AddRangeAsync([instance]);
        var useCase = new ExecuteDailyFlexibleTaskUseCase(new InMemoryDailyFlexibleExecutionRepository(), instances, templates, weeks, users, new NoOpUnitOfWork());

        var result = await useCase.ExecuteAsync(new ExecuteDailyFlexibleTaskCommand(instance.Id, owner.Id, week.StartDate));

        result.TaskInstanceId.Should().Be(instance.Id);
        instance.IsCompleted.Should().BeTrue();
    }

    [Fact]
    public async Task Non_flexible_task_is_rejected()
    {
        var owner = new User("Julie");
        var users = new InMemoryUserRepository(); users.Seed(owner);
        var week = new Week(new WeekKey(2026, 1)); var weeks = new InMemoryWeekRepository(); await weeks.AddAsync(week);
        var template = new TaskTemplate("Courses", owner.Id, 20, FrequencyType.Weekly);
        var templates = new InMemoryTaskTemplateRepository(); templates.Seed(template);
        var instance = new TaskInstance(template.Id, null, week, owner.Id, week.StartDate, week.EndDate);
        var instances = new InMemoryTaskInstanceRepository(); await instances.AddRangeAsync([instance]);
        var useCase = new ExecuteDailyFlexibleTaskUseCase(new InMemoryDailyFlexibleExecutionRepository(), instances, templates, weeks, users, new NoOpUnitOfWork());

        var action = () => useCase.ExecuteAsync(new ExecuteDailyFlexibleTaskCommand(instance.Id, owner.Id, week.StartDate));

        await action.Should().ThrowAsync<DomainValidationException>();
    }

    [Fact]
    public async Task Closed_week_is_rejected()
    {
        var owner = new User("Julie");
        var users = new InMemoryUserRepository(); users.Seed(owner);
        var week = new Week(new WeekKey(2026, 1));
        week.Close();
        var weeks = new InMemoryWeekRepository(); await weeks.AddAsync(week);
        var template = new TaskTemplate("Plantes", owner.Id, 20, FrequencyType.DailyFlexible);
        var templates = new InMemoryTaskTemplateRepository(); templates.Seed(template);
        var instance = new TaskInstance(template.Id, null, week, owner.Id, week.StartDate, week.EndDate);
        var instances = new InMemoryTaskInstanceRepository(); await instances.AddRangeAsync([instance]);
        var useCase = new ExecuteDailyFlexibleTaskUseCase(new InMemoryDailyFlexibleExecutionRepository(), instances, templates, weeks, users, new NoOpUnitOfWork());

        var action = () => useCase.ExecuteAsync(new ExecuteDailyFlexibleTaskCommand(instance.Id, owner.Id, week.StartDate));

        await action.Should().ThrowAsync<DomainValidationException>();
    }

    [Fact]
    public async Task Unassigned_non_admin_user_is_rejected()
    {
        var owner = new User("Julie");
        var other = new User("Marc");
        var users = new InMemoryUserRepository(); users.Seed(owner); users.Seed(other);
        var week = new Week(new WeekKey(2026, 1)); var weeks = new InMemoryWeekRepository(); await weeks.AddAsync(week);
        var template = new TaskTemplate("Plantes", owner.Id, 20, FrequencyType.DailyFlexible);
        var templates = new InMemoryTaskTemplateRepository(); templates.Seed(template);
        var instance = new TaskInstance(template.Id, null, week, owner.Id, week.StartDate, week.EndDate);
        var instances = new InMemoryTaskInstanceRepository(); await instances.AddRangeAsync([instance]);
        var useCase = new ExecuteDailyFlexibleTaskUseCase(new InMemoryDailyFlexibleExecutionRepository(), instances, templates, weeks, users, new NoOpUnitOfWork());

        var action = () => useCase.ExecuteAsync(new ExecuteDailyFlexibleTaskCommand(instance.Id, other.Id, week.StartDate));

        await action.Should().ThrowAsync<ForbiddenOperationException>();
    }

    [Fact]
    public async Task Multiple_executions_same_week_are_recorded()
    {
        var owner = new User("Julie");
        var users = new InMemoryUserRepository(); users.Seed(owner);
        var week = new Week(new WeekKey(2026, 1)); var weeks = new InMemoryWeekRepository(); await weeks.AddAsync(week);
        var template = new TaskTemplate("Plantes", owner.Id, 20, FrequencyType.DailyFlexible);
        var templates = new InMemoryTaskTemplateRepository(); templates.Seed(template);
        var instance = new TaskInstance(template.Id, null, week, owner.Id, week.StartDate, week.EndDate);
        var instances = new InMemoryTaskInstanceRepository(); await instances.AddRangeAsync([instance]);
        var executionRepository = new InMemoryDailyFlexibleExecutionRepository();
        var useCase = new ExecuteDailyFlexibleTaskUseCase(executionRepository, instances, templates, weeks, users, new NoOpUnitOfWork());

        await useCase.ExecuteAsync(new ExecuteDailyFlexibleTaskCommand(instance.Id, owner.Id, week.StartDate));
        await useCase.ExecuteAsync(new ExecuteDailyFlexibleTaskCommand(instance.Id, owner.Id, week.StartDate.AddDays(1)));

        var executions = await executionRepository.GetByTaskInstanceAsync(instance.Id);
        executions.Should().HaveCount(2);
    }
}
