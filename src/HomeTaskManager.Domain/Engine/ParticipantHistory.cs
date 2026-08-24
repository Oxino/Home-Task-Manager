namespace HomeTaskManager.Domain.Engine;

/// <summary>A participant's charge and repetition history over the equity window (BR-053).</summary>
public sealed record ParticipantHistory(
    Guid UserId,
    decimal TotalScore,
    int TotalAssignedCount,
    IReadOnlyDictionary<Guid, int> OccurrencesByTaskTemplate);
