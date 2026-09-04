namespace HomeTaskManager.Domain.Enums;

/// <summary>How a <see cref="HomeTaskManager.Domain.Entities.TaskTemplate"/> recurs, driving weekly instance generation.</summary>
public enum FrequencyType
{
    /// <summary>Generates 7 fixed daily instances; single-owner unless DistributeDailyMandatory is true.</summary>
    DailyMandatory,
    /// <summary>No fixed instances: logged via <see cref="HomeTaskManager.Domain.Entities.DailyFlexibleExecution"/> whenever done, any day.</summary>
    DailyFlexible,
    /// <summary>One instance per week, due date per WeeklyScheduleType.</summary>
    Weekly,
    /// <summary>One instance per month, due date per MonthlyScheduleType/MonthlyDay.</summary>
    Monthly,
    /// <summary>A single non-recurring instance.</summary>
    OneTime,
    /// <summary>One instance on each of several selectable week days (ActiveWeekDays).</summary>
    WeeklyFlexible
}