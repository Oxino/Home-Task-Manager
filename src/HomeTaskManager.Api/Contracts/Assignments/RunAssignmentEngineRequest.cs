namespace HomeTaskManager.Api.Contracts.Assignments;

/// <summary>POST /api/assignments/run body. TriggeredByUserId is currently unused server-side (the real actor is read from the auth cookie); kept for backward-compat.</summary>
public sealed record RunAssignmentEngineRequest(int Year, int WeekNumber, Guid TriggeredByUserId);
