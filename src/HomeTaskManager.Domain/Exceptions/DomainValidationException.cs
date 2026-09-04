namespace HomeTaskManager.Domain.Exceptions;

/// <summary>Raised when a domain entity's invariant is violated (e.g. invalid date range, out-of-bounds score). Maps to a 400 response at the API boundary.</summary>
public sealed class DomainValidationException(string message) : Exception(message)
{
}