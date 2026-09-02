using HomeTaskManager.Domain.Entities;

namespace HomeTaskManager.Domain.Engine;

/// <summary>The three factors multiplied together to obtain a task's CalculatedScore (BR-052).</summary>
public sealed record TaskScoreBreakdown(decimal ManualDifficultyScore, decimal PriorityMultiplier, decimal DurationFactor)
{
    public decimal Score => ManualDifficultyScore * PriorityMultiplier * DurationFactor;
}

/// <summary>BR-052: CalculatedScore = ManualDifficultyScore x PriorityMultiplier x DurationFactor.</summary>
public static class TaskScoreCalculator
{
    public static decimal Calculate(TaskTemplate taskTemplate, AssignmentEngineSettings settings) =>
        CalculateBreakdown(taskTemplate, settings).Score;

    public static TaskScoreBreakdown CalculateBreakdown(TaskTemplate taskTemplate, AssignmentEngineSettings settings)
    {
        var priorityMultiplier = settings.PriorityMultipliers.TryGetValue(taskTemplate.Priority, out var multiplier)
            ? multiplier
            : 1.0m;

        var durationFactor = taskTemplate.EstimatedDurationMinutes.HasValue
            ? (decimal)taskTemplate.EstimatedDurationMinutes.Value / settings.ReferenceDurationMinutes
            : 1.0m;

        return new TaskScoreBreakdown(taskTemplate.ManualDifficultyScore, priorityMultiplier, durationFactor);
    }
}
