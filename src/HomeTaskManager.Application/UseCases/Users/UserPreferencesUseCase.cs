using HomeTaskManager.Application.Abstractions.Repositories;
using HomeTaskManager.Application.Abstractions;
using HomeTaskManager.Domain.Entities;
using HomeTaskManager.Domain.Exceptions;

namespace HomeTaskManager.Application.UseCases.Users;

public sealed record UserPreferencesDto(Guid Id, Guid UserId, string NotificationPreference, string DateDisplayFormat);
public sealed record UpdateUserPreferencesCommand(Guid UserId, string NotificationPreference, string DateDisplayFormat);

/// <summary>Reads/updates a user's display preferences, lazily creating the row on first access (defaults set by the entity constructor).</summary>
public interface IUserPreferencesUseCase
{
    Task<UserPreferencesDto> GetAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<UserPreferencesDto> UpdateAsync(UpdateUserPreferencesCommand command, CancellationToken cancellationToken = default);
}

public sealed class UserPreferencesUseCase(IUserPreferencesRepository repository, IAuditLogger auditLogger, IUnitOfWork unitOfWork) : IUserPreferencesUseCase
{
    public async Task<UserPreferencesDto> GetAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var preferences = await repository.GetByUserIdAsync(userId, cancellationToken);
        if (preferences is null)
        {
            // Auto-provision default preferences on first read instead of requiring a separate setup step.
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
        preferences.SetNotificationPreference(command.NotificationPreference);
        preferences.SetDateDisplayFormat(command.DateDisplayFormat);
        await auditLogger.LogAsync(AuditEntityTypes.UserPreferences, preferences.Id, AuditActions.Updated, command.UserId,
            newValue: $"NotificationPreference={preferences.NotificationPreference}; DateDisplayFormat={preferences.DateDisplayFormat}", cancellationToken: cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ToDto(preferences);
    }

    private static UserPreferencesDto ToDto(UserPreferences preferences) => new(preferences.Id, preferences.UserId, preferences.NotificationPreference, preferences.DateDisplayFormat);
}