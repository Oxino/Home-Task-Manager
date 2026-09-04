namespace HomeTaskManager.Application.UseCases.Settings;

/// <summary>API-facing mirror of <see cref="HomeTaskManager.Domain.Engine.AssignmentEngineSettings"/>.</summary>
public sealed record AssignmentEngineSettingsDto(
    decimal LowPriorityMultiplier,
    decimal NormalPriorityMultiplier,
    decimal CriticalPriorityMultiplier,
    int ReferenceDurationMinutes,
    int EquityWindowWeeks,
    decimal RepetitionPenaltyWeight,
    string StrategyName);
