namespace HomeTaskManager.Application.UseCases.Assignments;

public sealed record ExplanationCandidateDto(Guid UserId, string DisplayName, decimal? Value, bool Selected, string? Reason);

public sealed record ExplanationStepDto(string Kind, string Title, string Summary, IReadOnlyList<ExplanationCandidateDto>? Candidates);

public sealed record AssignmentExplanationDto(
    Guid TaskInstanceId,
    Guid AssignedUserId,
    string EngineName,
    string SummaryText,
    DateTime GeneratedAt,
    IReadOnlyList<ExplanationStepDto> Steps);
