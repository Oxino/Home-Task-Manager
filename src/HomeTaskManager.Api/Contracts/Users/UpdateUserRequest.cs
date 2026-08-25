namespace HomeTaskManager.Api.Contracts.Users;

public sealed record UpdateUserRequest(string DisplayName, bool IsAdmin, int AvailabilityPercentage, bool IsActive);