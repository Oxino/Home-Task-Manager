using HomeTaskManager.Domain.Enums;

namespace HomeTaskManager.Application.UseCases.TaskTemplates;

/// <summary>API-facing mirror of <see cref="HomeTaskManager.Domain.Entities.TaskTemplate"/>; enums are serialized as strings for the frontend.</summary>
public sealed record TaskTemplateDto(
    Guid Id,
    string Title,
    string? Description,
    int ManualDifficultyScore,
    string Priority,
    int? EstimatedDurationMinutes,
    string FrequencyType,
    bool IsActive,
    Guid CreatedByUserId,
    IReadOnlyList<DayOfWeek> ActiveWeekDays,
    MonthlyScheduleType MonthlyScheduleType,
    int? MonthlyDay,
    WeeklyScheduleType WeeklyScheduleType,
    bool DistributeDailyMandatory);
