using HomeTaskManager.Domain.Enums;
using HomeTaskManager.Domain.Exceptions;

namespace HomeTaskManager.Domain.Entities;

public sealed class TaskTemplate
{
    private const int MinimumDifficultyScore = 1;
    private const int MaximumDifficultyScore = 100;
    private const int MinimumMonthDay = 1;
    private const int MaximumMonthDay = 31;

    private TaskTemplate()
    {
    }

    public TaskTemplate(
        string title,
        Guid createdByUserId,
        int manualDifficultyScore,
        FrequencyType frequencyType,
        TaskPriority priority = TaskPriority.Normal,
        string? description = null,
        int? estimatedDurationMinutes = null,
        IEnumerable<DayOfWeek>? activeWeekDays = null,
        MonthlyScheduleType monthlyScheduleType = MonthlyScheduleType.StartOfMonth,
        int? monthlyDay = null,
        WeeklyScheduleType weeklyScheduleType = WeeklyScheduleType.EndOfWeek,
        bool distributeDailyMandatory = true)
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
        IsActive = true;
        CreatedByUserId = createdByUserId;

        Rename(title);
        ChangeDescription(description);
        ChangeDifficulty(manualDifficultyScore);
        ChangeFrequency(frequencyType);
        if (frequencyType == FrequencyType.WeeklyFlexible && activeWeekDays is null)
        {
            throw new DomainValidationException("A fluid weekly task requires at least one active day.");
        }

        SetActiveWeekDays(activeWeekDays ?? []);
        SetMonthlySchedule(monthlyScheduleType, monthlyDay);
        WeeklyScheduleType = weeklyScheduleType;
        DistributeDailyMandatory = distributeDailyMandatory;
        ChangePriority(priority);
        ChangeEstimatedDuration(estimatedDurationMinutes);
        UpdatedAt = CreatedAt;
    }

    public Guid Id { get; private set; }

    public string Title { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public int ManualDifficultyScore { get; private set; }

    public TaskPriority Priority { get; private set; }

    public int? EstimatedDurationMinutes { get; private set; }

    public FrequencyType FrequencyType { get; private set; }

    public string ActiveWeekDays { get; private set; } = string.Empty;

    public MonthlyScheduleType MonthlyScheduleType { get; private set; }

    public int? MonthlyDay { get; private set; }

    public WeeklyScheduleType WeeklyScheduleType { get; private set; }

    // BR-038: for DailyMandatory only; true spreads the 7 instances across participants, false keeps a single weekly owner.
    public bool DistributeDailyMandatory { get; private set; } = true;

    public bool IsActive { get; private set; }

    public Guid CreatedByUserId { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime UpdatedAt { get; private set; }

    public void Rename(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new DomainValidationException("A task title is required.");
        }

        Title = title.Trim();
        Touch();
    }

    public void ChangeDescription(string? description)
    {
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        Touch();
    }

    public void ChangeDifficulty(int manualDifficultyScore)
    {
        if (manualDifficultyScore is < MinimumDifficultyScore or > MaximumDifficultyScore)
        {
            throw new DomainValidationException("Manual difficulty score must be between 1 and 100.");
        }

        ManualDifficultyScore = manualDifficultyScore;
        Touch();
    }

    public void ChangePriority(TaskPriority priority)
    {
        Priority = priority;
        Touch();
    }

    public void ChangeFrequency(FrequencyType frequencyType)
    {
        FrequencyType = frequencyType;
        Touch();
    }

    public IReadOnlyList<DayOfWeek> GetActiveWeekDays() =>
        ActiveWeekDays.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(value => Enum.TryParse<DayOfWeek>(value, out var day) ? day : (DayOfWeek?)null)
            .Where(day => day.HasValue)
            .Select(day => day!.Value)
            .ToList();

    public void SetActiveWeekDays(IEnumerable<DayOfWeek> days)
    {
        var normalized = days.Distinct().OrderBy(day => day).ToList();
        if (FrequencyType == FrequencyType.WeeklyFlexible && normalized.Count == 0)
        {
            throw new DomainValidationException("A fluid weekly task requires at least one active day.");
        }

        ActiveWeekDays = string.Join(',', normalized);
        Touch();
    }

    public void SetMonthlySchedule(MonthlyScheduleType scheduleType, int? day)
    {
        if (FrequencyType == FrequencyType.Monthly && scheduleType == MonthlyScheduleType.SpecificDay && day is < MinimumMonthDay or > MaximumMonthDay)
        {
            throw new DomainValidationException("A specific monthly day must be between 1 and 31.");
        }

        MonthlyScheduleType = scheduleType;
        MonthlyDay = scheduleType == MonthlyScheduleType.SpecificDay ? day : null;
        Touch();
    }

    public void SetWeeklySchedule(WeeklyScheduleType scheduleType)
    {
        WeeklyScheduleType = scheduleType;
        Touch();
    }

    public void SetDistributeDailyMandatory(bool distribute)
    {
        DistributeDailyMandatory = distribute;
        Touch();
    }

    public void ChangeEstimatedDuration(int? estimatedDurationMinutes)
    {
        if (estimatedDurationMinutes is <= 0)
        {
            throw new DomainValidationException("Estimated duration must be at least one minute.");
        }

        EstimatedDurationMinutes = estimatedDurationMinutes;
        Touch();
    }

    public void SetActive(bool isActive)
    {
        IsActive = isActive;
        Touch();
    }

    private void Touch() => UpdatedAt = DateTime.UtcNow;
}