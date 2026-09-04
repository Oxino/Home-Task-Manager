using FluentAssertions;
using HomeTaskManager.Application.Exceptions;
using HomeTaskManager.Application.UseCases.Users;
using HomeTaskManager.Domain.Entities;
using HomeTaskManager.Domain.Exceptions;

namespace HomeTaskManager.Application.Tests;

/// <summary>Verifies UpdateUserUseCase's admin-only authorization and DisplayName-uniqueness re-check.</summary>
public sealed class UpdateUserUseCaseTests
{
    [Fact]
    public async Task Admin_can_update_role_availability_and_status()
    {
        var repository = new InMemoryUserRepository();
        var admin = new User("Admin", isAdmin: true);
        var participant = new User("Julie");
        repository.Seed(admin);
        repository.Seed(participant);
        var useCase = new UpdateUserUseCase(repository, new NoOpAuditLogger(), new NoOpUnitOfWork());

        var result = await useCase.ExecuteAsync(new UpdateUserCommand(participant.Id, admin.Id, "Julie", true, 50, false));

        result.IsAdmin.Should().BeTrue();
        result.AvailabilityPercentage.Should().Be(50);
        result.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task Non_admin_cannot_update_a_profile()
    {
        var repository = new InMemoryUserRepository();
        var participant = new User("Julie");
        var otherParticipant = new User("Nicolas");
        repository.Seed(participant);
        repository.Seed(otherParticipant);
        var useCase = new UpdateUserUseCase(repository, new NoOpAuditLogger(), new NoOpUnitOfWork());

        var action = () => useCase.ExecuteAsync(new UpdateUserCommand(otherParticipant.Id, participant.Id, "Nicolas", false, 100, true));

        await action.Should().ThrowAsync<ForbiddenOperationException>();
    }

    [Fact]
    public async Task Cannot_rename_a_profile_to_an_existing_name()
    {
        var repository = new InMemoryUserRepository();
        var admin = new User("Admin", isAdmin: true);
        var first = new User("Julie");
        var second = new User("Nicolas");
        repository.Seed(admin);
        repository.Seed(first);
        repository.Seed(second);
        var useCase = new UpdateUserUseCase(repository, new NoOpAuditLogger(), new NoOpUnitOfWork());

        var action = () => useCase.ExecuteAsync(new UpdateUserCommand(second.Id, admin.Id, "Julie", false, 100, true));

        await action.Should().ThrowAsync<DomainValidationException>();
    }
}
