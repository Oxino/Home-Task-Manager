using FluentAssertions;
using HomeTaskManager.Application.Abstractions;
using HomeTaskManager.Application.Abstractions.Repositories;
using HomeTaskManager.Application.UseCases.Users;
using HomeTaskManager.Domain.Entities;
using HomeTaskManager.Domain.Exceptions;

namespace HomeTaskManager.Application.Tests;

public sealed class InMemoryUserRepository : IUserRepository
{
    private readonly List<User> _users = [];

    public void Seed(User user) => _users.Add(user);

    public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_users.SingleOrDefault(user => user.Id == id));

    public Task<IReadOnlyList<User>> GetActiveAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<User>>(_users.Where(user => user.IsActive).ToList());

    public Task<bool> ExistsByDisplayNameAsync(string displayName, CancellationToken cancellationToken = default) =>
        Task.FromResult(_users.Any(user => user.DisplayName == displayName));

    public Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        _users.Add(user);
        return Task.CompletedTask;
    }
}

public sealed class NoOpUnitOfWork : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);
}

public sealed class NoOpAuditLogger : IAuditLogger
{
    public Task LogAsync(
        string entityType,
        Guid entityId,
        string action,
        Guid performedByUserId,
        string? oldValue = null,
        string? newValue = null,
        CancellationToken cancellationToken = default) => Task.CompletedTask;
}

public class CreateUserUseCaseTests
{
    [Fact]
    public async Task Creates_a_profile_when_display_name_is_unique()
    {
        var repository = new InMemoryUserRepository();
        var useCase = new CreateUserUseCase(repository, new NoOpAuditLogger(), new NoOpUnitOfWork());

        var profile = await useCase.ExecuteAsync(new CreateUserCommand("Julie", false, 100));

        profile.DisplayName.Should().Be("Julie");
        profile.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task Rejects_a_duplicate_display_name()
    {
        var repository = new InMemoryUserRepository();
        repository.Seed(new User("Julie"));
        var useCase = new CreateUserUseCase(repository, new NoOpAuditLogger(), new NoOpUnitOfWork());

        var action = () => useCase.ExecuteAsync(new CreateUserCommand("Julie", false, 100));

        await action.Should().ThrowAsync<DomainValidationException>();
    }
}

public class ListActiveProfilesUseCaseTests
{
    [Fact]
    public async Task Returns_only_active_profiles()
    {
        var repository = new InMemoryUserRepository();
        var activeUser = new User("Julie");
        var inactiveUser = new User("Nicolas");
        inactiveUser.SetActive(false);
        repository.Seed(activeUser);
        repository.Seed(inactiveUser);
        var useCase = new ListActiveProfilesUseCase(repository);

        var profiles = await useCase.ExecuteAsync();

        profiles.Should().ContainSingle().Which.DisplayName.Should().Be("Julie");
    }
}

public class SelectProfileUseCaseTests
{
    [Fact]
    public async Task Selects_an_active_profile()
    {
        var repository = new InMemoryUserRepository();
        var user = new User("Julie");
        repository.Seed(user);
        var useCase = new SelectProfileUseCase(repository);

        var profile = await useCase.ExecuteAsync(user.Id);

        profile.Id.Should().Be(user.Id);
    }

    [Fact]
    public async Task Rejects_selecting_an_inactive_profile()
    {
        var repository = new InMemoryUserRepository();
        var user = new User("Julie");
        user.SetActive(false);
        repository.Seed(user);
        var useCase = new SelectProfileUseCase(repository);

        var action = () => useCase.ExecuteAsync(user.Id);

        await action.Should().ThrowAsync<DomainValidationException>();
    }

    [Fact]
    public async Task Rejects_selecting_an_unknown_profile()
    {
        var repository = new InMemoryUserRepository();
        var useCase = new SelectProfileUseCase(repository);

        var action = () => useCase.ExecuteAsync(Guid.NewGuid());

        await action.Should().ThrowAsync<DomainValidationException>();
    }
}
