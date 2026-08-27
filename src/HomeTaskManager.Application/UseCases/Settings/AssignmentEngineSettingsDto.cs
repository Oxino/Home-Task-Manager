namespace HomeTaskManager.Application.UseCases.Settings;

public sealed record AssignmentEngineSettingsDto(
    decimal LowPriorityMultiplier,
    decimal NormalPriorityMultiplier,
    decimal CriticalPriorityMultiplier,
    int ReferenceDurationMinutes,
    int EquityWindowWeeks,
    decimal RepetitionPenaltyWeight,
    string StrategyName);
