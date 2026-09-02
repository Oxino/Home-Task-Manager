using System.ComponentModel.DataAnnotations;
using HomeTaskManager.Domain.Enums;

namespace HomeTaskManager.Api.Contracts.TaskTemplates;

public sealed record UpdateTaskTemplateRequest(
    Guid RequestingUserId,
    [Required, StringLength(200, MinimumLength = 1)] string Title,
    [StringLength(2000)] string? Description,
    int ManualDifficultyScore,
    TaskPriority Priority,
    FrequencyType FrequencyType,
    int? EstimatedDurationMinutes,
    IReadOnlyCollection<DayOfWeek>? ActiveWeekDays = null,
    MonthlyScheduleType MonthlyScheduleType = MonthlyScheduleType.StartOfMonth,
    int? MonthlyDay = null,
    WeeklyScheduleType WeeklyScheduleType = WeeklyScheduleType.EndOfWeek,
    bool DistributeDailyMandatory = true);
