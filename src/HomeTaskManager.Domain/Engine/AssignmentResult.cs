namespace HomeTaskManager.Domain.Engine;

public sealed record TaskAssignment(Guid TaskInstanceId, Guid AssignedUserId);

public sealed record AssignmentResult(IReadOnlyList<TaskAssignment> Assignments);
