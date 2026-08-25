namespace HomeTaskManager.Api.Contracts.TaskInstances;

public sealed record CancelTaskInstanceRequest(Guid RequestingUserId);