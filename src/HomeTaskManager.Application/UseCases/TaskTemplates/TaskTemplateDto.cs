namespace HomeTaskManager.Application.UseCases.TaskTemplates;

public sealed record TaskTemplateDto(
    Guid Id,
    string Title,
    string? Description,
    int ManualDifficultyScore,
    string Priority,
    int? EstimatedDurationMinutes,
    string FrequencyType,
    bool IsActive,
    Guid CreatedByUserId);
