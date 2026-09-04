namespace HomeTaskManager.Domain.Enums;

/// <summary>ADR-011: a week starts Active (assignments/completion editable) and becomes Closed (frozen, BR-043) once, never reopened.</summary>
public enum WeekStatus
{
    Active,
    Closed
}