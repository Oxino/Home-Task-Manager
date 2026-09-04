namespace HomeTaskManager.Api.Contracts.TaskInstances;

/// <summary>PUT /api/task-instances/{id}/assignment body.</summary>
public sealed record ReassignTaskInstanceRequest(Guid AssignedUserId);