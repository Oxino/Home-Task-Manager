namespace HomeTaskManager.Application.Abstractions;

/// <summary>Abstraction over the current time so use cases stay testable (fakeable in unit tests instead of calling DateTime.UtcNow directly).</summary>
public interface IClock
{
    DateTime UtcNow { get; }
}