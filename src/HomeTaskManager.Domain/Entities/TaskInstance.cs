using HomeTaskManager.Domain.Enums;
using HomeTaskManager.Domain.Exceptions;

namespace HomeTaskManager.Domain.Entities;

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

    public void SetCompletion(bool isCompleted, Week week)
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
        CompletedAt = isCompleted ? DateTime.UtcNow : null;
        Status = isCompleted ? TaskInstanceStatus.Completed : TaskInstanceStatus.Pending;
        UpdatedAt = DateTime.UtcNow;
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

    public void Reassign(Guid userId)
    {
        if (Status == TaskInstanceStatus.Cancelled)
        {
            throw new DomainValidationException("A cancelled task instance cannot be assigned.");
        }

        AssignedUserId = userId;
        UpdatedAt = DateTime.UtcNow;
    }
}