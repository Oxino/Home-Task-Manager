using System.ComponentModel.DataAnnotations;

namespace HomeTaskManager.Api.Contracts.Settings;

public sealed record UpdateAssignmentEngineSettingsRequest(
    Guid RequestingUserId,
    decimal LowPriorityMultiplier,
    decimal NormalPriorityMultiplier,
    decimal CriticalPriorityMultiplier,
    int ReferenceDurationMinutes,
    int EquityWindowWeeks,
    decimal RepetitionPenaltyWeight,
    [Required, StringLength(50, MinimumLength = 1)] string StrategyName = "INTELLIGENT_V1");
