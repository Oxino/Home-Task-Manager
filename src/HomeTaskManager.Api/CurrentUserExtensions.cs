using System.Security.Claims;

namespace HomeTaskManager.Api;

public static class CurrentUserExtensions
{
    public static Guid GetCurrentUserId(this ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var userId)
            ? userId
            : throw new InvalidOperationException("The authenticated profile has no valid identifier.");
    }
}