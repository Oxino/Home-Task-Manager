using HomeTaskManager.Application.Abstractions.Repositories;

namespace HomeTaskManager.Application.UseCases.Users;

public interface IListActiveProfilesUseCase
{
    Task<IReadOnlyList<UserProfileDto>> ExecuteAsync(CancellationToken cancellationToken = default);
}

public sealed class ListActiveProfilesUseCase(IUserRepository userRepository) : IListActiveProfilesUseCase
{
    public async Task<IReadOnlyList<UserProfileDto>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var users = await userRepository.GetActiveAsync(cancellationToken);
        return users.Select(UserProfileMapper.ToDto).ToList();
    }
}
