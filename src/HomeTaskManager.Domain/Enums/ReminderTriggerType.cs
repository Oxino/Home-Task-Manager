namespace HomeTaskManager.Domain.Enums;

/// <summary>Anchor point a <see cref="HomeTaskManager.Domain.Entities.Reminder"/>'s offset is computed from.</summary>
public enum ReminderTriggerType
{
    /// <summary>Offset from the end of the task instance's due day.</summary>
    EndOfDay,
    /// <summary>Offset from the end of the current week.</summary>
    EndOfWeek
}
