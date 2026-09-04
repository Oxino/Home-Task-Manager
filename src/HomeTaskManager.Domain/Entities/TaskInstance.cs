using HomeTaskManager.Domain.Enums;
using HomeTaskManager.Domain.Exceptions;

namespace HomeTaskManager.Domain.Entities;

/// <summary>
/// A single concrete occurrence of a <see cref="TaskTemplate"/> generated for one week (ADR-001): e.g. a daily
/// mandatory template produces 7 instances. Editing the template never alters instances already generated (BR-047).
/// </summary>
public sealed class TaskInstance
{
    private TaskInstance()
    {
    }

    public TaskInstance(
        Guid taskTemplateId,
        Guid? assignmentRunId,
        Week week,
        Guid? assignedUserId,
        DateOnly plannedDate,
        DateOnly dueDate,
        bool isCarryOver = false,
        Guid? parentTaskInstanceId = null)
    {
        if (plannedDate < week.StartDate || plannedDate > week.EndDate)
        {
            throw new DomainValidationException("The planned date must belong to the instance week.");
        }

        if (dueDate < plannedDate)
        {
            throw new DomainValidationException("The due date cannot precede the planned date.");
        }

        Id = Guid.NewGuid();
        TaskTemplateId = taskTemplateId;
        AssignmentRunId = assignmentRunId;
        WeekId = week.Id;
        AssignedUserId = assignedUserId;
        PlannedDate = plannedDate;
        DueDate = dueDate;
        Status = TaskInstanceStatus.Pending;
        IsCarryOver = isCarryOver;
        ParentTaskInstanceId = parentTaskInstanceId;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = CreatedAt;
    }

    public Guid Id { get; private set; }

    public Guid TaskTemplateId { get; private set; }

    public Guid? AssignmentRunId { get; private set; }

    public Guid WeekId { get; private set; }

    public Guid? AssignedUserId { get; private set; }

    public bool IsAssigned => AssignedUserId.HasValue;

    public DateOnly PlannedDate { get; private set; }

    public DateOnly DueDate { get; private set; }

    public TaskInstanceStatus Status { get; private set; }

    public bool IsCompleted { get; private set; }

    public DateTime? CompletedAt { get; private set; }

    public bool IsCarryOver { get; private set; }

    public Guid? ParentTaskInstanceId { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime UpdatedAt { get; private set; }

    /// <summary>Sets completion using the current time; use <see cref="SetCompletionAt"/> to backdate (e.g. imports/tests).</summary>
    public void SetCompletion(bool isCompleted, Week week)
    {
        SetCompletionAt(isCompleted, week, DateTime.UtcNow);
    }

    /// <summary>BR-043/BR-055: completion is freely toggled while the week is active, but frozen once the week is closed.</summary>
    public void SetCompletionAt(bool isCompleted, Week week, DateTime completedAt)
    {
        if (week.Id != WeekId)
        {
            throw new DomainValidationException("The provided week does not own this task instance.");
        }

        if (week.Status == WeekStatus.Closed)
        {
            throw new DomainValidationException("Completion cannot be changed after the week is closed.");
        }

        if (Status == TaskInstanceStatus.Cancelled)
        {
            throw new DomainValidationException("A cancelled task instance cannot be completed.");
        }

        IsCompleted = isCompleted;
        CompletedAt = isCompleted ? completedAt : null;
        Status = isCompleted ? TaskInstanceStatus.Completed : TaskInstanceStatus.Pending;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>Used when re-importing/reconstructing history: restores a past completion timestamp without going through the normal completion flow.</summary>
    public void RestoreCompletionAt(DateTime completedAt)
    {
        if (!IsCompleted) throw new DomainValidationException("Only completed task instances can restore a completion date.");
        CompletedAt = completedAt;
    }

    public void Cancel()
    {
        if (Status == TaskInstanceStatus.Completed)
        {
            throw new DomainValidationException("A completed task instance cannot be cancelled.");
        }

        Status = TaskInstanceStatus.Cancelled;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>First assignment of this instance by an <see cref="AssignmentRun"/> (the assignment engine).</summary>
    public void Assign(Guid assignmentRunId, Guid userId)
    {
        if (Status == TaskInstanceStatus.Cancelled)
        {
            throw new DomainValidationException("A cancelled task instance cannot be assigned.");
        }

        AssignmentRunId = assignmentRunId;
        AssignedUserId = userId;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>Manual admin reassignment to a different user (ADR-013), keeping the original AssignmentRunId.</summary>
    public void Reassign(Guid userId)
    {
        if (Status == TaskInstanceStatus.Completed || IsCompleted)
        {
            throw new DomainValidationException("A completed task instance cannot be reassigned.");
        }
        if (Status == TaskInstanceStatus.Cancelled)
        {
            throw new DomainValidationException("A cancelled task instance cannot be assigned.");
        }

        AssignedUserId = userId;
        UpdatedAt = DateTime.UtcNow;
    }
}