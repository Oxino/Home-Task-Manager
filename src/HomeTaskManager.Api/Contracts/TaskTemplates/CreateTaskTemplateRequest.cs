using HomeTaskManager.Domain.Enums;

namespace HomeTaskManager.Api.Contracts.TaskTemplates;

public sealed record CreateTaskTemplateRequest(
    string Title,
    string? Description,
    int ManualDifficultyScore,
    FrequencyType FrequencyType,
    TaskPriority Priority,
    int? EstimatedDurationMinutes,
    Guid CreatedByUserId,
    IReadOnlyCollection<DayOfWeek>? ActiveWeekDays = null,
    MonthlyScheduleType MonthlyScheduleType = MonthlyScheduleType.StartOfMonth,
    int? MonthlyDay = null);
