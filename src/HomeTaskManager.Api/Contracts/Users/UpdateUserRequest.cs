using System.ComponentModel.DataAnnotations;

namespace HomeTaskManager.Api.Contracts.Users;

/// <summary>PUT /api/users/{id} body (admin-only profile edition).</summary>
public sealed record UpdateUserRequest(
    [Required, StringLength(100, MinimumLength = 1)] string DisplayName,
    bool IsAdmin,
    int AvailabilityPercentage,
    bool IsActive);