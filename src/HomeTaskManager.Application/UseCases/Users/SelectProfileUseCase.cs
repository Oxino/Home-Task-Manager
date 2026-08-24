using HomeTaskManager.Application.Abstractions.Repositories;
using HomeTaskManager.Domain.Exceptions;

namespace HomeTaskManager.Application.UseCases.Users;

public interface ISelectProfileUseCase
{
    Task<UserProfileDto> ExecuteAsync(Guid userId, CancellationToken cancellationToken = default);
}

public sealed class SelectProfileUseCase(IUserRepository userRepository) : ISelectProfileUseCase
{
    public async Task<UserProfileDto> ExecuteAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new DomainValidationException("The requested profile does not exist.");

        // BR-004: an inactive profile cannot be selected to open a session.
        if (!user.IsActive)
        {
            throw new DomainValidationException("The requested profile is inactive.");
        }

        return UserProfileMapper.ToDto(user);
    }
}
