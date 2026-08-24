using HomeTaskManager.Domain.Exceptions;

namespace HomeTaskManager.Domain.Entities;

public sealed class SystemSetting
{
    private SystemSetting()
    {
    }

    public SystemSetting(string key, string value, Guid updatedByUserId)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new DomainValidationException("A system setting key is required.");
        }

        Id = Guid.NewGuid();
        Key = key.Trim();
        SetValue(value, updatedByUserId);
    }

    public Guid Id { get; private set; }

    public string Key { get; private set; } = string.Empty;

    public string Value { get; private set; } = string.Empty;

    public Guid UpdatedByUserId { get; private set; }

    public DateTime UpdatedAt { get; private set; }

    public void SetValue(string value, Guid updatedByUserId)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainValidationException("A system setting value is required.");
        }

        Value = value.Trim();
        UpdatedByUserId = updatedByUserId;
        UpdatedAt = DateTime.UtcNow;
    }
}
