using HomeTaskManager.Domain.Enums;
using HomeTaskManager.Domain.Exceptions;

namespace HomeTaskManager.Domain.Entities;

public sealed class Reminder
{
    private Reminder() { }

    public Reminder(Guid taskTemplateId, ReminderTriggerType triggerType, int offsetValue, ReminderOffsetUnit offsetUnit)
    {
        if (offsetValue < 0) throw new DomainValidationException("Reminder offset cannot be negative.");
        Id = Guid.NewGuid();
        TaskTemplateId = taskTemplateId;
        TriggerType = triggerType;
        OffsetValue = offsetValue;
        OffsetUnit = offsetUnit;
        IsEnabled = true;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = CreatedAt;
    }

    public Guid Id { get; private set; }
    public Guid TaskTemplateId { get; private set; }
    public ReminderTriggerType TriggerType { get; private set; }
    public int OffsetValue { get; private set; }
    public ReminderOffsetUnit OffsetUnit { get; private set; }
    public bool IsEnabled { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public void SetEnabled(bool enabled)
    {
        IsEnabled = enabled;
        UpdatedAt = DateTime.UtcNow;
    }
}
