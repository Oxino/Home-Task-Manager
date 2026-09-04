using HomeTaskManager.Application.Abstractions;
using HomeTaskManager.Application.Abstractions.Repositories;
using HomeTaskManager.Application.Exceptions;
using HomeTaskManager.Domain.Exceptions;

namespace HomeTaskManager.Application.UseCases.Users;

public sealed record UpdateUserCommand(Guid UserId, Guid RequestingUserId, string DisplayName, bool IsAdmin, int AvailabilityPercentage, bool IsActive);

/// <summary>Admin-only: edits any profile's name/role/availability/active flag.</summary>
public interface IUpdateUserUseCase
{
    Task<UserProfileDto> ExecuteAsync(UpdateUserCommand command, CancellationToken cancellationToken = default);
}

public sealed class UpdateUserUseCase(
    IUserRepository userRepository,
    IAuditLogger auditLogger,
    IUnitOfWork unitOfWork) : IUpdateUserUseCase
{
    public async Task<UserProfileDto> ExecuteAsync(UpdateUserCommand command, CancellationToken cancellationToken = default)
    {
        var requestingUser = await userRepository.GetByIdAsync(command.RequestingUserId, cancellationToken)
            ?? throw new DomainValidationException("The requesting profile does not exist.");
        if (!requestingUser.IsAdmin) throw new ForbiddenOperationException("Only an administrator can modify profiles.");

        var user = await userRepository.GetByIdAsync(command.UserId, cancellationToken)
            ?? throw new DomainValidationException("The requested profile does not exist.");
        if (!string.Equals(user.DisplayName, command.DisplayName, StringComparison.OrdinalIgnoreCase) &&
            await userRepository.ExistsByDisplayNameAsync(command.DisplayName, cancellationToken))
            throw new DomainValidationException($"A profile named '{command.DisplayName}' already exists.");

        var oldValue = $"DisplayName={user.DisplayName}; IsAdmin={user.IsAdmin}; IsActive={user.IsActive}; Availability={user.AvailabilityPercentage}";
        user.Rename(command.DisplayName);
        user.ChangeRole(command.IsAdmin);
        user.ChangeAvailability(command.AvailabilityPercentage);
        user.SetActive(command.IsActive);
        var newValue = $"DisplayName={user.DisplayName}; IsAdmin={user.IsAdmin}; IsActive={user.IsActive}; Availability={user.AvailabilityPercentage}";
        await auditLogger.LogAsync(AuditEntityTypes.User, user.Id, AuditActions.Updated, requestingUser.Id, oldValue, newValue, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return UserProfileMapper.ToDto(user);
    }
}