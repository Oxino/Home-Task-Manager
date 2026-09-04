namespace HomeTaskManager.Domain.Engine;

/// <summary>ExplanationSteps is null only when a strategy does not (yet) produce a decision trace (BR-033).</summary>
public sealed record TaskAssignment(Guid TaskInstanceId, Guid AssignedUserId, IReadOnlyList<ExplanationStep>? ExplanationSteps = null);

/// <summary>Output of an <see cref="IAssignmentStrategy"/> run: one assignment per task instance that was assigned.</summary>
public sealed record AssignmentResult(IReadOnlyList<TaskAssignment> Assignments);
