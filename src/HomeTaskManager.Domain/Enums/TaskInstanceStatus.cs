namespace HomeTaskManager.Domain.Enums;

/// <summary>Lifecycle state of a <see cref="HomeTaskManager.Domain.Entities.TaskInstance"/>.</summary>
public enum TaskInstanceStatus
{
    Pending,
    Completed,
    /// <summary>Terminal: a cancelled instance can never be completed, assigned or reassigned again.</summary>
    Cancelled
}