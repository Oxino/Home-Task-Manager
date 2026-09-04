namespace HomeTaskManager.Api.Contracts.TaskInstances;

/// <summary>POST /api/task-instances/{id}/executions body: logs one execution of a DailyFlexible task on the given date.</summary>
public sealed record ExecuteDailyFlexibleTaskRequest(DateOnly ExecutionDate);