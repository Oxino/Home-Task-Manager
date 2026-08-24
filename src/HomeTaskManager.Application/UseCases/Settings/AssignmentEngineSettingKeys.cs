namespace HomeTaskManager.Application.UseCases.Settings;

/// <summary>SystemSetting keys backing the BR-058 administrable assignment engine parameters.</summary>
public static class AssignmentEngineSettingKeys
{
    public const string LowPriorityMultiplier = "AssignmentEngine.PriorityMultiplier.Low";
    public const string NormalPriorityMultiplier = "AssignmentEngine.PriorityMultiplier.Normal";
    public const string CriticalPriorityMultiplier = "AssignmentEngine.PriorityMultiplier.Critical";
    public const string ReferenceDurationMinutes = "AssignmentEngine.ReferenceDurationMinutes";
    public const string EquityWindowWeeks = "AssignmentEngine.EquityWindowWeeks";
    public const string RepetitionPenaltyWeight = "AssignmentEngine.RepetitionPenaltyWeight";
}
