using FluentAssertions;
using HomeTaskManager.Application.Abstractions.Repositories;
using HomeTaskManager.Application.Exceptions;
using HomeTaskManager.Application.UseCases.Assignments;
using HomeTaskManager.Application.UseCases.Settings;
using HomeTaskManager.Domain.Engine;
using HomeTaskManager.Domain.Entities;
using HomeTaskManager.Domain.Enums;
using HomeTaskManager.Domain.Exceptions;
using HomeTaskManager.Domain.ValueObjects;

namespace HomeTaskManager.Application.Tests;

public sealed class InMemoryAssignmentRunRepository : IAssignmentRunRepository
{
    private readonly List<AssignmentRun> _runs = [];

    public Task<bool> ExistsForWeekAsync(Guid weekId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_runs.Any(run => run.WeekId == weekId));

    public Task<IReadOnlyList<AssignmentRun>> GetByWeekAsync(Guid weekId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<AssignmentRun>>(_runs.Where(run => run.WeekId == weekId).OrderByDescending(run => run.GeneratedAt).ToList());

    public Task AddAsync(AssignmentRun assignmentRun, CancellationToken cancellationToken = default)
    {
        _runs.Add(assignmentRun);
        return Task.CompletedTask;
    }
}

public class RunAssignmentEngineUseCaseTests
{
    private static RunAssignmentEngineUseCase CreateUseCase(
        InMemoryUserRepository userRepository,
        InMemoryWeekRepository weekRepository,
        InMemoryTaskInstanceRepository instanceRepository,
        IAssignmentStrategy? strategy = null,
        InMemoryTaskTemplateRepository? templateRepository = null,
        INotificationRepository? notificationRepository = null,
        IUserPreferencesRepository? userPreferencesRepository = null) =>
        new(weekRepository, userRepository, templateRepository ?? new InMemoryTaskTemplateRepository(), instanceRepository,
            new InMemoryAssignmentRunRepository(), strategy ?? new RandomStrategy(new Random(1)),
            new AssignmentEngineSettingsProvider(new InMemorySystemSettingRepository()), new NoOpAuditLogger(), new NoOpUnitOfWork(), notificationRepository, userPreferencesRepository);

    private static async Task<(Week Week, TaskInstance Instance)> SeedGeneratedWeekAsync(
        InMemoryWeekRepository weekRepository, InMemoryTaskInstanceRepository instanceRepository)
    {
        var week = new Week(new WeekKey(2026, 1));
        await weekRepository.AddAsync(week);
        var instance = new TaskInstance(Guid.NewGuid(), null, week, null, week.StartDate, week.EndDate);
        await instanceRepository.AddRangeAsync([instance]);
        return (week, instance);
    }

    [Fact]
    public async Task Admin_can_run_the_engine_and_assign_instances()
    {
        var userRepository = new InMemoryUserRepository();
        var admin = new User("Nicolas", isAdmin: true);
        var participant = new User("Julie");
        userRepository.Seed(admin);
        userRepository.Seed(participant);
        var weekRepository = new InMemoryWeekRepository();
        var instanceRepository = new InMemoryTaskInstanceRepository();
        await SeedGeneratedWeekAsync(weekRepository, instanceRepository);
        var useCase = CreateUseCase(userRepository, weekRepository, instanceRepository);

        var result = await useCase.ExecuteAsync(new RunAssignmentEngineCommand(2026, 1, admin.Id));

        result.Instances.Should().ContainSingle().Which.AssignedUserId.Should().NotBeNull();
    }

    [Fact]
    public async Task Assignment_creates_a_notification_for_the_assigned_participant()
    {
        var userRepository = new InMemoryUserRepository();
        var admin = new User("Nicolas", isAdmin: true);
        var participant = new User("Julie");
        userRepository.Seed(admin);
        userRepository.Seed(participant);
        var weekRepository = new InMemoryWeekRepository();
        var instanceRepository = new InMemoryTaskInstanceRepository();
        var templateRepository = new InMemoryTaskTemplateRepository();
        var template = new TaskTemplate("Aspirateur", participant.Id, 40, FrequencyType.Weekly);
        templateRepository.Seed(template);
        var week = new Week(new WeekKey(2026, 1));
        await weekRepository.AddAsync(week);
        await instanceRepository.AddRangeAsync([new TaskInstance(template.Id, null, week, null, week.StartDate, week.EndDate)]);
        var notificationRepository = new InMemoryNotificationRepository();
        var useCase = CreateUseCase(userRepository, weekRepository, instanceRepository, templateRepository: templateRepository, notificationRepository: notificationRepository);

        var result = await useCase.ExecuteAsync(new RunAssignmentEngineCommand(2026, 1, admin.Id));

        var assignedUserId = result.Instances.Single().AssignedUserId!.Value;
        (await notificationRepository.GetByUserIdAsync(assignedUserId)).Should().ContainSingle().Which.Title.Should().Be("Nouvelle tâche affectée");
    }

    [Fact]
    public async Task Daily_mandatory_assignment_creates_one_notification_for_the_week()
    {
        var userRepository = new InMemoryUserRepository();
        var admin = new User("Nicolas", isAdmin: true);
        var participant = new User("Julie");
        userRepository.Seed(admin);
        userRepository.Seed(participant);
        var weekRepository = new InMemoryWeekRepository();
        var instanceRepository = new InMemoryTaskInstanceRepository();
        var templateRepository = new InMemoryTaskTemplateRepository();
        var template = new TaskTemplate("Vaisselle quotidienne", participant.Id, 40, FrequencyType.DailyMandatory);
        templateRepository.Seed(template);
        var week = new Week(new WeekKey(2026, 1));
        await weekRepository.AddAsync(week);
        var instances = Enumerable.Range(0, 7).Select(day => new TaskInstance(template.Id, null, week, null, week.StartDate.AddDays(day), week.StartDate.AddDays(day)));
        await instanceRepository.AddRangeAsync(instances);
        var notificationRepository = new InMemoryNotificationRepository();
        var useCase = CreateUseCase(userRepository, weekRepository, instanceRepository, new IntelligentV1Strategy(), templateRepository, notificationRepository);

        await useCase.ExecuteAsync(new RunAssignmentEngineCommand(2026, 1, admin.Id));

        var assignedUserId = (await instanceRepository.GetByWeekAsync(week.Id)).First().AssignedUserId!.Value;
        (await notificationRepository.GetByUserIdAsync(assignedUserId)).Should().ContainSingle();
    }

    [Fact]
    public async Task Non_admin_cannot_run_the_engine()
    {
        var userRepository = new InMemoryUserRepository();
        var regularUser = new User("Julie");
        userRepository.Seed(regularUser);
        var weekRepository = new InMemoryWeekRepository();
        var instanceRepository = new InMemoryTaskInstanceRepository();
        await SeedGeneratedWeekAsync(weekRepository, instanceRepository);
        var useCase = CreateUseCase(userRepository, weekRepository, instanceRepository);

        var action = () => useCase.ExecuteAsync(new RunAssignmentEngineCommand(2026, 1, regularUser.Id));

        await action.Should().ThrowAsync<ForbiddenOperationException>();
    }

    [Fact]
    public async Task Rejects_running_on_a_week_that_was_not_generated()
    {
        var userRepository = new InMemoryUserRepository();
        var admin = new User("Nicolas", isAdmin: true);
        userRepository.Seed(admin);
        var useCase = CreateUseCase(userRepository, new InMemoryWeekRepository(), new InMemoryTaskInstanceRepository());

        var action = () => useCase.ExecuteAsync(new RunAssignmentEngineCommand(2026, 1, admin.Id));

        await action.Should().ThrowAsync<DomainValidationException>();
    }

    [Fact]
    public async Task Records_a_run_with_no_assignments_when_there_is_no_active_participant()
    {
        var userRepository = new InMemoryUserRepository();
        var admin = new User("Nicolas", isAdmin: true);
        admin.SetActive(false);
        userRepository.Seed(admin);
        var weekRepository = new InMemoryWeekRepository();
        var instanceRepository = new InMemoryTaskInstanceRepository();
        await SeedGeneratedWeekAsync(weekRepository, instanceRepository);
        var useCase = CreateUseCase(userRepository, weekRepository, instanceRepository);

        var result = await useCase.ExecuteAsync(new RunAssignmentEngineCommand(2026, 1, admin.Id));

        result.Instances.Should().ContainSingle().Which.AssignedUserId.Should().BeNull();
        result.Run.Notes.Should().NotBeNull();
    }

    [Fact]
    public async Task Carried_over_instances_are_not_reassigned()
    {
        var userRepository = new InMemoryUserRepository();
        var admin = new User("Nicolas", isAdmin: true);
        var originalOwner = new User("Julie");
        userRepository.Seed(admin);
        userRepository.Seed(originalOwner);
        var weekRepository = new InMemoryWeekRepository();
        var week = new Week(new WeekKey(2026, 1));
        await weekRepository.AddAsync(week);
        var instanceRepository = new InMemoryTaskInstanceRepository();
        var carriedOver = new TaskInstance(
            Guid.NewGuid(), null, week, originalOwner.Id, week.StartDate, week.EndDate, isCarryOver: true);
        await instanceRepository.AddRangeAsync([carriedOver]);
        var useCase = CreateUseCase(userRepository, weekRepository, instanceRepository);

        await useCase.ExecuteAsync(new RunAssignmentEngineCommand(2026, 1, admin.Id));

        carriedOver.AssignedUserId.Should().Be(originalOwner.Id);
    }
}
