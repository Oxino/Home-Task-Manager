namespace HomeTaskManager.Api.Contracts.TaskInstances;

/// <summary>PUT /api/task-instances/{id}/cancel body. RequestingUserId is currently unused server-side (the real actor is read from the auth cookie); kept for backward-compat.</summary>
public sealed record CancelTaskInstanceRequest(Guid RequestingUserId);