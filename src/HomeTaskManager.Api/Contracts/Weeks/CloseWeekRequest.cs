namespace HomeTaskManager.Api.Contracts.Weeks;

/// <summary>POST /api/weeks/{year}/{weekNumber}/close body.</summary>
public sealed record CloseWeekRequest(bool CancelIncompleteTasks = false);
