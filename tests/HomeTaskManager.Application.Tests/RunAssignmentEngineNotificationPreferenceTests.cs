using FluentAssertions;
using HomeTaskManager.Application.Abstractions.Repositories;
using HomeTaskManager.Application.UseCases.Assignments;
using HomeTaskManager.Application.UseCases.Settings;
using HomeTaskManager.Domain.Entities;
using HomeTaskManager.Domain.Enums;
using HomeTaskManager.Domain.ValueObjects;

namespace HomeTaskManager.Application.Tests;

public sealed class InMemoryUserPreferencesRepository : IUserPreferencesRepository
{
    private readonly List<UserPreferences> _preferences = [];
    public Task<UserPreferences?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default) => Task.FromResult(_preferences.SingleOrDefault(preferences => preferences.UserId == userId));
    public Task AddAsync(UserPreferences preferences, CancellationToken cancellationToken = default) { _preferences.Add(preferences); return Task.CompletedTask; }
    public void Seed(UserPreferences preferences) => _preferences.Add(preferences);
}

public sealed class RunAssignmentEngineNotificationPreferenceTests
{
    [Fact]
    public async Task Assignment_does_not_create_notification_when_participant_disabled_them()
    {
        var users = new InMemoryUserRepository();
        var admin = new User("Admin", isAdmin: true);
        var participant = new User("Julie");
        users.Seed(admin); users.Seed(participant);
        var preferences = new UserPreferences(participant.Id); preferences.SetNotificationsEnabled(false);
        var preferencesRepository = new InMemoryUserPreferencesRepository(); preferencesRepository.Seed(preferences);
        var week = new Week(new WeekKey(2026, 1)); var weeks = new InMemoryWeekRepository(); await weeks.AddAsync(week);
        var template = new TaskTemplate("Aspirateur", participant.Id, 40, FrequencyType.Weekly); var templates = new InMemoryTaskTemplateRepository(); templates.Seed(template);
        var instance = new TaskInstance(template.Id, null, week, null, week.StartDate, week.EndDate); var instances = new InMemoryTaskInstanceRepository(); await instances.AddRangeAsync([instance]);
        var notifications = new InMemoryNotificationRepository();
        var useCase = new RunAssignmentEngineUseCase(weeks, users, templates, instances, new InMemoryAssignmentRunRepository(), new Domain.Engine.IntelligentV1Strategy(), new AssignmentEngineSettingsProvider(new InMemorySystemSettingRepository()), new NoOpAuditLogger(), new NoOpUnitOfWork(), notifications, preferencesRepository);

        await useCase.ExecuteAsync(new RunAssignmentEngineCommand(2026, 1, admin.Id));

        (await notifications.GetByUserIdAsync(participant.Id)).Should().BeEmpty();
    }
}
