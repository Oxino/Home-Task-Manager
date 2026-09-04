namespace HomeTaskManager.Domain.Enums;

/// <summary>BR-052/BR-056: multiplies a task's score via AssignmentEngineSettings.PriorityMultipliers, default Normal.</summary>
public enum TaskPriority
{
    Low,
    Normal,
    Critical
}