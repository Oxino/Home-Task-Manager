using HomeTaskManager.Application.Abstractions.Repositories;
using HomeTaskManager.Application.Abstractions;
using HomeTaskManager.Domain.Entities;
using HomeTaskManager.Domain.Exceptions;

namespace HomeTaskManager.Application.UseCases.Users;

public sealed record UserPreferencesDto(Guid Id, Guid UserId, bool NotificationsEnabled);
public sealed record UpdateUserPreferencesCommand(Guid UserId, bool NotificationsEnabled);

public interface IUserPreferencesUseCase
{
    Task<UserPreferencesDto> GetAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<UserPreferencesDto> UpdateAsync(UpdateUserPreferencesCommand command, CancellationToken cancellationToken = default);
}

public sealed class UserPreferencesUseCase(IUserPreferencesRepository repository, IUnitOfWork unitOfWork) : IUserPreferencesUseCase
{
    public async Task<UserPreferencesDto> GetAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var preferences = await repository.GetByUserIdAsync(userId, cancellationToken);
        if (preferences is null)
        {
            preferences = new UserPreferences(userId);
            await repository.AddAsync(preferences, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        return ToDto(preferences);
    }

    public async Task<UserPreferencesDto> UpdateAsync(UpdateUserPreferencesCommand command, CancellationToken cancellationToken = default)
    {
        var preferences = await repository.GetByUserIdAsync(command.UserId, cancellationToken);
        if (preferences is null)
        {
            preferences = new UserPreferences(command.UserId);
            await repository.AddAsync(preferences, cancellationToken);
        }
        preferences.SetNotificationsEnabled(command.NotificationsEnabled);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ToDto(preferences);
    }

    private static UserPreferencesDto ToDto(UserPreferences preferences) => new(preferences.Id, preferences.UserId, preferences.NotificationsEnabled);
}