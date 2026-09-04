using System.ComponentModel.DataAnnotations;
using HomeTaskManager.Domain.Enums;

namespace HomeTaskManager.Api.Contracts.TaskTemplates;

/// <summary>POST /api/task-templates body.</summary>
public sealed record CreateTaskTemplateRequest(
    [Required, StringLength(200, MinimumLength = 1)] string Title,
    [StringLength(2000)] string? Description,
    int ManualDifficultyScore,
    FrequencyType FrequencyType,
    TaskPriority Priority,
    int? EstimatedDurationMinutes,
    Guid CreatedByUserId,
    IReadOnlyCollection<DayOfWeek>? ActiveWeekDays = null,
    MonthlyScheduleType MonthlyScheduleType = MonthlyScheduleType.StartOfMonth,
    int? MonthlyDay = null,
    WeeklyScheduleType WeeklyScheduleType = WeeklyScheduleType.EndOfWeek,
    bool DistributeDailyMandatory = true);
