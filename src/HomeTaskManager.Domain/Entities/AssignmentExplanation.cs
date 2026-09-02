namespace HomeTaskManager.Domain.Entities;

/// <summary>BR-033: the persisted decision trace behind one task instance's assignment.</summary>
public sealed class AssignmentExplanation
{
    private AssignmentExplanation()
    {
    }

    public AssignmentExplanation(Guid taskInstanceId, Guid assignmentRunId, Guid assignedUserId, string engineName, string summaryText, string stepsJson)
    {
        Id = Guid.NewGuid();
        TaskInstanceId = taskInstanceId;
        AssignmentRunId = assignmentRunId;
        AssignedUserId = assignedUserId;
        EngineName = engineName;
        SummaryText = summaryText;
        StepsJson = stepsJson;
        GeneratedAt = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }

    public Guid TaskInstanceId { get; private set; }

    public Guid AssignmentRunId { get; private set; }

    public Guid AssignedUserId { get; private set; }

    public string EngineName { get; private set; } = string.Empty;

    public string SummaryText { get; private set; } = string.Empty;

    /// <summary>Serialized ordered list of ExplanationStep (Domain.Engine), rendered as the decision path in the UI.</summary>
    public string StepsJson { get; private set; } = string.Empty;

    public DateTime GeneratedAt { get; private set; }
}
