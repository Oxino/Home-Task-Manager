using System.ComponentModel.DataAnnotations;

namespace HomeTaskManager.Api.Contracts.Users;

/// <summary>POST /api/users body: creates a new profile (ADR-003, no email/password).</summary>
public sealed record CreateUserRequest(
    [Required, StringLength(100, MinimumLength = 1)] string DisplayName,
    bool IsAdmin,
    int AvailabilityPercentage);
