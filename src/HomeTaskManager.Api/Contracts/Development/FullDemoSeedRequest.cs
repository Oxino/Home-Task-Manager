namespace HomeTaskManager.Api.Contracts.Development;

/// <summary>POST /api/development/reset-and-seed-full-demo body: number of past weeks (6-20, see DevelopmentController) to seed with realistic history.</summary>
public sealed record FullDemoSeedRequest(int Weeks = 10);
