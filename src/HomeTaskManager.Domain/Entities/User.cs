using HomeTaskManager.Domain.Exceptions;

namespace HomeTaskManager.Domain.Entities;

/// <summary>
/// A household member. Only two roles exist, carried by <see cref="IsAdmin"/> (ADR-010); there is no password/email,
/// authentication is by profile selection (ADR-003/ADR-006) and uniqueness is enforced on <see cref="DisplayName"/>.
/// <see cref="AvailabilityPercentage"/> feeds the assignment engine's load-normalization (e.g. a part-time participant).
/// </summary>
public sealed class User
{
    private const int MinimumAvailabilityPercentage = 0;
    private const int MaximumAvailabilityPercentage = 100;

    private User()
    {
    }

    public User(string displayName, bool isAdmin = false, int availabilityPercentage = 100)
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
        IsActive = true;
        IsAdmin = isAdmin;

        Rename(displayName);
        ChangeAvailability(availabilityPercentage);
        UpdatedAt = CreatedAt;
    }

    public Guid Id { get; private set; }

    public string DisplayName { get; private set; } = string.Empty;

    public bool IsAdmin { get; private set; }

    public bool IsActive { get; private set; }

    public int AvailabilityPercentage { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime UpdatedAt { get; private set; }

    public void Rename(string displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new DomainValidationException("A user display name is required.");
        }

        DisplayName = displayName.Trim();
        Touch();
    }

    public void ChangeAvailability(int availabilityPercentage)
    {
        if (availabilityPercentage is < MinimumAvailabilityPercentage or > MaximumAvailabilityPercentage)
        {
            throw new DomainValidationException("Availability percentage must be between 0 and 100.");
        }

        AvailabilityPercentage = availabilityPercentage;
        Touch();
    }

    public void SetActive(bool isActive)
    {
        IsActive = isActive;
        Touch();
    }

    public void ChangeRole(bool isAdmin)
    {
        IsAdmin = isAdmin;
        Touch();
    }

    private void Touch() => UpdatedAt = DateTime.UtcNow;
}