namespace HomeTaskManager.Api.Contracts.Users;

/// <summary>PUT /api/users/preferences body.</summary>
public sealed record UpdateUserPreferencesRequest(string NotificationPreference = "All", string DateDisplayFormat = "Weekday");