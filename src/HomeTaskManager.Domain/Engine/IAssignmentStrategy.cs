namespace HomeTaskManager.Domain.Engine;

/// <summary>Contract for pluggable assignment algorithms (ADR-002). Add new strategies without changing existing ones.</summary>
public interface IAssignmentStrategy
{
    string Name { get; }

    AssignmentResult Assign(AssignmentContext context);
}
