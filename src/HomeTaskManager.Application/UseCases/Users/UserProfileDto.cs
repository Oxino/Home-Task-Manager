namespace HomeTaskManager.Application.UseCases.Users;

public sealed record UserProfileDto(Guid Id, string DisplayName, bool IsAdmin, bool IsActive, int AvailabilityPercentage);
