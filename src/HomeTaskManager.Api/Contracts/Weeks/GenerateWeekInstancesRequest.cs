namespace HomeTaskManager.Api.Contracts.Weeks;

/// <summary>POST /api/weeks/generate body.</summary>
public sealed record GenerateWeekInstancesRequest(int Year, int WeekNumber);
