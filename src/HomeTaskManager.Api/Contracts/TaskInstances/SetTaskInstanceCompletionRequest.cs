namespace HomeTaskManager.Api.Contracts.TaskInstances;

public sealed record SetTaskInstanceCompletionRequest(Guid RequestingUserId, bool IsCompleted);
