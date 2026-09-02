using System.ComponentModel.DataAnnotations;

namespace HomeTaskManager.Api.Contracts.Users;

public sealed record UpdateUserRequest(
    [Required, StringLength(100, MinimumLength = 1)] string DisplayName,
    bool IsAdmin,
    int AvailabilityPercentage,
    bool IsActive);