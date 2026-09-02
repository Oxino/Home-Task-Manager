using HomeTaskManager.Application.Abstractions;
using HomeTaskManager.Application.Abstractions.Repositories;
using HomeTaskManager.Application.Exceptions;
using HomeTaskManager.Domain.Entities;
using HomeTaskManager.Domain.Exceptions;

namespace HomeTaskManager.Application.UseCases.Users;

public sealed record CreateUserCommand(string DisplayName, bool IsAdmin, int AvailabilityPercentage, bool RequestingUserIsAdmin = false);

public interface ICreateUserUseCase
{
    Task<UserProfileDto> ExecuteAsync(CreateUserCommand command, CancellationToken cancellationToken = default);
}

public sealed class CreateUserUseCase(IUserRepository userRepository, IAuditLogger auditLogger, IUnitOfWork unitOfWork) : ICreateUserUseCase
{
    public async Task<UserProfileDto> ExecuteAsync(CreateUserCommand command, CancellationToken cancellationToken = default)
    {
        // BR-002: DisplayName must be unique across active and inactive profiles.
        if (await userRepository.ExistsByDisplayNameAsync(command.DisplayName, cancellationToken))
        {
            throw new DomainValidationException($"A profile named '{command.DisplayName}' already exists.");
        }

        if (command.IsAdmin && !command.RequestingUserIsAdmin)
        {
            // BR-017 bootstrap: self-attribution of the admin role is only allowed while no admin exists yet.
            if (await userRepository.ExistsAnyAdminAsync(cancellationToken))
            {
                throw new ForbiddenOperationException("Only an existing administrator can create another administrator profile.");
            }
        }

        var user = new User(command.DisplayName, command.IsAdmin, command.AvailabilityPercentage);
        await userRepository.AddAsync(user, cancellationToken);

        // BR-017: no acting admin exists yet for the very first profile, so the profile self-attributes its creation.
        await auditLogger.LogAsync(
            AuditEntityTypes.User, user.Id, AuditActions.Created, user.Id,
            newValue: $"DisplayName={user.DisplayName}; IsAdmin={user.IsAdmin}", cancellationToken: cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return UserProfileMapper.ToDto(user);
    }
}
