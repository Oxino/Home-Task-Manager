namespace HomeTaskManager.Api.Contracts.Assignments;

public sealed record RunAssignmentEngineRequest(int Year, int WeekNumber, Guid TriggeredByUserId);
