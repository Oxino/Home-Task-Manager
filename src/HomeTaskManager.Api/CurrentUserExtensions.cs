using System.Security.Claims;

namespace HomeTaskManager.Api;

/// <summary>Extracts the authenticated user's id from the auth cookie's claims, used by every controller instead of trusting a client-supplied user id.</summary>
public static class CurrentUserExtensions
{
    public static Guid GetCurrentUserId(this ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var userId)
            ? userId
            // Should never happen for an authenticated request, but fail loudly rather than silently using Guid.Empty.
            : throw new InvalidOperationException("The authenticated profile has no valid identifier.");
    }
}