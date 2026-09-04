namespace HomeTaskManager.Domain.Engine;

/// <summary>Contract for pluggable assignment algorithms (ADR-002). Add new strategies without changing existing ones.</summary>
public interface IAssignmentStrategy
{
    /// <summary>Unique identifier persisted in settings/audit trails, e.g. "INTELLIGENT_V1" or "RANDOM".</summary>
    string Name { get; }

    /// <summary>Decides which participant gets each task instance in <paramref name="context"/>. Must be a pure function of the context (no I/O, no randomness seeded by wall-clock time) so runs stay explainable and reproducible.</summary>
    AssignmentResult Assign(AssignmentContext context);
}
