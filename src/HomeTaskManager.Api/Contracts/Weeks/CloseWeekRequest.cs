namespace HomeTaskManager.Api.Contracts.Weeks;

public sealed record CloseWeekRequest(bool CancelIncompleteTasks = false);
