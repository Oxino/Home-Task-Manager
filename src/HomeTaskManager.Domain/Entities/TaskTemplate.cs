using HomeTaskManager.Domain.Enums;
using HomeTaskManager.Domain.Exceptions;

namespace HomeTaskManager.Domain.Entities;

public sealed class TaskTemplate
{
    private const int MinimumDifficultyScore = 1;
    private const int MaximumDifficultyScore = 100;

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
        int? estimatedDurationMinutes = null)
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
        IsActive = true;
        CreatedByUserId = createdByUserId;

        Rename(title);
        ChangeDescription(description);
        ChangeDifficulty(manualDifficultyScore);
        ChangeFrequency(frequencyType);
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