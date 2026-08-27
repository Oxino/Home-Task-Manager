namespace HomeTaskManager.Api.Contracts.Settings;

public sealed record UpdateAssignmentEngineSettingsRequest(
    Guid RequestingUserId,
    decimal LowPriorityMultiplier,
    decimal NormalPriorityMultiplier,
    decimal CriticalPriorityMultiplier,
    int ReferenceDurationMinutes,
    int EquityWindowWeeks,
    decimal RepetitionPenaltyWeight,
    string StrategyName = "INTELLIGENT_V1");
