namespace HomeTaskManager.Api.Contracts.TaskInstances;

/// <summary>PUT /api/task-instances/{id}/completion body. RequestingUserId is currently unused server-side (the real actor is read from the auth cookie); kept for backward-compat.</summary>
public sealed record SetTaskInstanceCompletionRequest(Guid RequestingUserId, bool IsCompleted);
