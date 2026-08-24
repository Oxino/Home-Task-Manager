using FluentAssertions;
using HomeTaskManager.Application.Abstractions.Repositories;
using HomeTaskManager.Application.Exceptions;
using HomeTaskManager.Application.UseCases.Settings;
using HomeTaskManager.Domain.Entities;
using HomeTaskManager.Domain.Exceptions;

namespace HomeTaskManager.Application.Tests;

public sealed class InMemorySystemSettingRepository : ISystemSettingRepository
{
    private readonly List<SystemSetting> _settings = [];

    public Task<SystemSetting?> GetByKeyAsync(string key, CancellationToken cancellationToken = default) =>
        Task.FromResult(_settings.SingleOrDefault(setting => setting.Key == key));

    public Task<IReadOnlyList<SystemSetting>> GetAllAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<SystemSetting>>(_settings.ToList());

    public Task AddAsync(SystemSetting setting, CancellationToken cancellationToken = default)
    {
        _settings.Add(setting);
        return Task.CompletedTask;
    }
}

public class AssignmentEngineSettingsProviderTests
{
    [Fact]
    public async Task Falls_back_to_defaults_when_no_setting_is_persisted()
    {
        var provider = new AssignmentEngineSettingsProvider(new InMemorySystemSettingRepository());

        var settings = await provider.GetAsync();

        var defaults = Domain.Engine.AssignmentEngineSettings.Default;
        settings.ReferenceDurationMinutes.Should().Be(defaults.ReferenceDurationMinutes);
        settings.EquityWindowWeeks.Should().Be(defaults.EquityWindowWeeks);
        settings.RepetitionPenaltyWeight.Should().Be(defaults.RepetitionPenaltyWeight);
    }

    [Fact]
    public async Task Uses_the_persisted_value_when_present()
    {
        var repository = new InMemorySystemSettingRepository();
        await repository.AddAsync(new SystemSetting(AssignmentEngineSettingKeys.EquityWindowWeeks, "12", Guid.NewGuid()));
        var provider = new AssignmentEngineSettingsProvider(repository);

        var settings = await provider.GetAsync();

        settings.EquityWindowWeeks.Should().Be(12);
    }
}

public class GetAssignmentEngineSettingsUseCaseTests
{
    [Fact]
    public async Task Returns_the_current_settings()
    {
        var provider = new AssignmentEngineSettingsProvider(new InMemorySystemSettingRepository());
        var useCase = new GetAssignmentEngineSettingsUseCase(provider);

        var settings = await useCase.ExecuteAsync();

        settings.EquityWindowWeeks.Should().Be(8);
    }
}

public class UpdateAssignmentEngineSettingsUseCaseTests
{
    private static UpdateAssignmentEngineSettingsCommand ValidCommand(Guid requestingUserId) =>
        new(requestingUserId, 0.5m, 1.0m, 1.5m, 30, 8, 0.25m);

    private static UpdateAssignmentEngineSettingsUseCase CreateUseCase(
        ISystemSettingRepository settingRepository, InMemoryUserRepository userRepository) =>
        new(settingRepository, userRepository, new NoOpAuditLogger(), new NoOpUnitOfWork());

    [Fact]
    public async Task Admin_can_update_the_settings()
    {
        var userRepository = new InMemoryUserRepository();
        var admin = new User("Nicolas", isAdmin: true);
        userRepository.Seed(admin);
        var settingRepository = new InMemorySystemSettingRepository();
        var useCase = CreateUseCase(settingRepository, userRepository);

        var command = ValidCommand(admin.Id) with { EquityWindowWeeks = 12 };
        var result = await useCase.ExecuteAsync(command);

        result.EquityWindowWeeks.Should().Be(12);
        (await new AssignmentEngineSettingsProvider(settingRepository).GetAsync()).EquityWindowWeeks.Should().Be(12);
    }

    [Fact]
    public async Task Non_admin_cannot_update_the_settings()
    {
        var userRepository = new InMemoryUserRepository();
        var regularUser = new User("Julie");
        userRepository.Seed(regularUser);
        var useCase = CreateUseCase(new InMemorySystemSettingRepository(), userRepository);

        var action = () => useCase.ExecuteAsync(ValidCommand(regularUser.Id));

        await action.Should().ThrowAsync<ForbiddenOperationException>();
    }

    [Fact]
    public async Task Rejects_a_non_positive_priority_multiplier()
    {
        var userRepository = new InMemoryUserRepository();
        var admin = new User("Nicolas", isAdmin: true);
        userRepository.Seed(admin);
        var useCase = CreateUseCase(new InMemorySystemSettingRepository(), userRepository);

        var command = ValidCommand(admin.Id) with { NormalPriorityMultiplier = 0m };
        var action = () => useCase.ExecuteAsync(command);

        await action.Should().ThrowAsync<DomainValidationException>();
    }

    [Fact]
    public async Task Updating_twice_overwrites_the_previous_value()
    {
        var userRepository = new InMemoryUserRepository();
        var admin = new User("Nicolas", isAdmin: true);
        userRepository.Seed(admin);
        var settingRepository = new InMemorySystemSettingRepository();
        var useCase = CreateUseCase(settingRepository, userRepository);

        await useCase.ExecuteAsync(ValidCommand(admin.Id) with { EquityWindowWeeks = 12 });
        await useCase.ExecuteAsync(ValidCommand(admin.Id) with { EquityWindowWeeks = 4 });

        (await new AssignmentEngineSettingsProvider(settingRepository).GetAsync()).EquityWindowWeeks.Should().Be(4);
    }
}
