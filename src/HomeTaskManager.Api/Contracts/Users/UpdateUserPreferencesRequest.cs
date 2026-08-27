namespace HomeTaskManager.Api.Contracts.Users;

public sealed record UpdateUserPreferencesRequest(string NotificationPreference = "All", string DateDisplayFormat = "Weekday");