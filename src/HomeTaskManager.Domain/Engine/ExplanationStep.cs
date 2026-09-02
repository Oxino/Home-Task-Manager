namespace HomeTaskManager.Domain.Engine;

/// <summary>One eligible participant considered at a decision point, and whether it was retained.</summary>
public sealed record ExplanationCandidate(Guid UserId, string DisplayName, decimal? Value, bool Selected, string? Reason);

/// <summary>
/// One criterion applied by an assignment strategy to a task, in the order it was actually evaluated
/// (used to render the "why was this task assigned" decision path, BR-033).
/// </summary>
public sealed record ExplanationStep(string Kind, string Title, string Summary, IReadOnlyList<ExplanationCandidate>? Candidates = null);
