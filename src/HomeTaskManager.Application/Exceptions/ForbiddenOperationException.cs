namespace HomeTaskManager.Application.Exceptions;

/// <summary>Raised when an authenticated user attempts an action they don't have the rights for (e.g. editing someone else's task, BR-037). Maps to a 403 response at the API boundary.</summary>
public sealed class ForbiddenOperationException(string message) : Exception(message)
{
}
