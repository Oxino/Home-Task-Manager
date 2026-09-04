namespace HomeTaskManager.Application.UseCases.Assignments;

/// <summary>API-facing mirror of <see cref="HomeTaskManager.Domain.Engine.ExplanationCandidate"/>.</summary>
public sealed record ExplanationCandidateDto(Guid UserId, string DisplayName, decimal? Value, bool Selected, string? Reason);

/// <summary>API-facing mirror of <see cref="HomeTaskManager.Domain.Engine.ExplanationStep"/>.</summary>
public sealed record ExplanationStepDto(string Kind, string Title, string Summary, IReadOnlyList<ExplanationCandidateDto>? Candidates);

/// <summary>BR-033: full "why was this task assigned to this person" decision trace, sent to the frontend.</summary>
public sealed record AssignmentExplanationDto(
    Guid TaskInstanceId,
    Guid AssignedUserId,
    string EngineName,
    string SummaryText,
    DateTime GeneratedAt,
    IReadOnlyList<ExplanationStepDto> Steps);
