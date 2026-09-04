using HomeTaskManager.Domain.Entities;

namespace HomeTaskManager.Application.UseCases.Users;

internal static class UserProfileMapper
{
    /// <summary>Flat 1:1 projection, no derived fields needed.</summary>
    public static UserProfileDto ToDto(User user) =>
        new(user.Id, user.DisplayName, user.IsAdmin, user.IsActive, user.AvailabilityPercentage);
}
