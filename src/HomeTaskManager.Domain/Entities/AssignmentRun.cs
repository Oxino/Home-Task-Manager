namespace HomeTaskManager.Domain.Entities;

public sealed class AssignmentRun
{
    private AssignmentRun()
    {
    }

    public AssignmentRun(Guid weekId, string strategyName, Guid triggeredByUserId, bool isAutomatic, string? notes = null)
    {
        Id = Guid.NewGuid();
        WeekId = weekId;
        StrategyName = strategyName;
        TriggeredByUserId = triggeredByUserId;
        IsAutomatic = isAutomatic;
        Notes = notes;
        GeneratedAt = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }

    public Guid WeekId { get; private set; }

    public string StrategyName { get; private set; } = string.Empty;

    public Guid TriggeredByUserId { get; private set; }

    public bool IsAutomatic { get; private set; }

    public string? Notes { get; private set; }

    public DateTime GeneratedAt { get; private set; }
}
