using HomeTaskManager.Domain.Entities;

namespace HomeTaskManager.Domain.Engine;

/// <summary>BR-052: CalculatedScore = ManualDifficultyScore x PriorityMultiplier x DurationFactor.</summary>
public static class TaskScoreCalculator
{
    public static decimal Calculate(TaskTemplate taskTemplate, AssignmentEngineSettings settings)
    {
        var priorityMultiplier = settings.PriorityMultipliers.TryGetValue(taskTemplate.Priority, out var multiplier)
            ? multiplier
            : 1.0m;

        var durationFactor = taskTemplate.EstimatedDurationMinutes.HasValue
            ? (decimal)taskTemplate.EstimatedDurationMinutes.Value / settings.ReferenceDurationMinutes
            : 1.0m;

        return taskTemplate.ManualDifficultyScore * priorityMultiplier * durationFactor;
    }
}
