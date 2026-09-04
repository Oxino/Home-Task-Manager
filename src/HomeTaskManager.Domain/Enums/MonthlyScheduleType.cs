namespace HomeTaskManager.Domain.Enums;

/// <summary>When a Monthly <see cref="FrequencyType"/> task falls due within the month.</summary>
public enum MonthlyScheduleType
{
    StartOfMonth,
    MiddleOfMonth,
    EndOfMonth,
    /// <summary>Uses TaskTemplate.MonthlyDay (1-31) as the exact due day.</summary>
    SpecificDay
}