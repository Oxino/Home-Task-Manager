namespace HomeTaskManager.Application.Abstractions;

/// <summary>Well-known entity type and action tags, avoiding magic strings across audited use cases.</summary>
public static class AuditEntityTypes
{
    public const string User = "User";
    public const string TaskTemplate = "TaskTemplate";
    public const string Week = "Week";
    public const string TaskInstance = "TaskInstance";
    public const string AssignmentRun = "AssignmentRun";
    public const string AssignmentEngineSettings = "AssignmentEngineSettings";
    public const string UserPreferences = "UserPreferences";
}

/// <summary>Well-known action tags recorded alongside an <see cref="AuditEntityTypes"/> entry.</summary>
public static class AuditActions
{
    public const string Created = "Created";
    public const string Updated = "Updated";
    public const string Closed = "Closed";
    public const string CompletionChanged = "CompletionChanged";
    public const string Reassigned = "Reassigned";
    public const string Cancelled = "Cancelled";
    public const string CommentAdded = "CommentAdded";
    public const string Executed = "Executed";
    public const string Generated = "Generated";
    public const string Recovered = "Recovered";
}
