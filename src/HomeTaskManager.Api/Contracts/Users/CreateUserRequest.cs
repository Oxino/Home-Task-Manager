namespace HomeTaskManager.Api.Contracts.Users;

public sealed record CreateUserRequest(string DisplayName, bool IsAdmin, int AvailabilityPercentage);
