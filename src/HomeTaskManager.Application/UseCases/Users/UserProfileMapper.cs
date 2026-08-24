using HomeTaskManager.Domain.Entities;

namespace HomeTaskManager.Application.UseCases.Users;

internal static class UserProfileMapper
{
    public static UserProfileDto ToDto(User user) =>
        new(user.Id, user.DisplayName, user.IsAdmin, user.IsActive, user.AvailabilityPercentage);
}
