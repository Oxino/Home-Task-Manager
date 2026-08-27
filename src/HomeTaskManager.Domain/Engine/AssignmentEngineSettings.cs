using HomeTaskManager.Domain.Enums;

namespace HomeTaskManager.Domain.Engine;

/// <summary>Administrable engine parameters (BR-058); defaults match ASSIGNMENT_ENGINE.md.</summary>
public sealed record AssignmentEngineSettings(
    IReadOnlyDictionary<TaskPriority, decimal> PriorityMultipliers,
    int ReferenceDurationMinutes,
    int EquityWindowWeeks,
    decimal RepetitionPenaltyWeight,
    string StrategyName = "INTELLIGENT_V1")
{
    public static AssignmentEngineSettings Default { get; } = new(
        new Dictionary<TaskPriority, decimal>
        {
            [TaskPriority.Low] = 0.5m,
            [TaskPriority.Normal] = 1.0m,
            [TaskPriority.Critical] = 1.5m,
        },
        ReferenceDurationMinutes: 30,
        EquityWindowWeeks: 8,
        RepetitionPenaltyWeight: 0.25m,
        StrategyName: "INTELLIGENT_V1");
}
