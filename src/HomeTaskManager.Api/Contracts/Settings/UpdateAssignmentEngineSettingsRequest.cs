using System.ComponentModel.DataAnnotations;

namespace HomeTaskManager.Api.Contracts.Settings;

/// <summary>PUT /api/settings/assignment-engine body (BR-058 admin-tunable parameters).</summary>
public sealed record UpdateAssignmentEngineSettingsRequest(
    Guid RequestingUserId,
    decimal LowPriorityMultiplier,
    decimal NormalPriorityMultiplier,
    decimal CriticalPriorityMultiplier,
    int ReferenceDurationMinutes,
    int EquityWindowWeeks,
    decimal RepetitionPenaltyWeight,
    [Required, StringLength(50, MinimumLength = 1)] string StrategyName = "INTELLIGENT_V1");
