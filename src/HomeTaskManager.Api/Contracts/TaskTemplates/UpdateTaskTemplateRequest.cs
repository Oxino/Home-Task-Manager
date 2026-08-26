using HomeTaskManager.Domain.Enums;

namespace HomeTaskManager.Api.Contracts.TaskTemplates;

public sealed record UpdateTaskTemplateRequest(
    Guid RequestingUserId,
    string Title,
    string? Description,
    int ManualDifficultyScore,
    TaskPriority Priority,
    FrequencyType FrequencyType,
    int? EstimatedDurationMinutes,
    IReadOnlyCollection<DayOfWeek>? ActiveWeekDays = null,
    MonthlyScheduleType MonthlyScheduleType = MonthlyScheduleType.StartOfMonth,
    int? MonthlyDay = null,
    WeeklyScheduleType WeeklyScheduleType = WeeklyScheduleType.EndOfWeek);
