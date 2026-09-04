namespace HomeTaskManager.Api.Contracts.Development;

/// <summary>POST /api/development/seed-history body: number of past weeks to generate and run the assignment engine on.</summary>
public sealed record SeedHistoryRequest(int Weeks = 20);