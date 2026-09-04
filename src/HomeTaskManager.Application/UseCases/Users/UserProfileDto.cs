namespace HomeTaskManager.Application.UseCases.Users;

/// <summary>API-facing mirror of <see cref="HomeTaskManager.Domain.Entities.User"/>.</summary>
public sealed record UserProfileDto(Guid Id, string DisplayName, bool IsAdmin, bool IsActive, int AvailabilityPercentage);
