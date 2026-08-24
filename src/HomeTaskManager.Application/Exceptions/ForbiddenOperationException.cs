namespace HomeTaskManager.Application.Exceptions;

public sealed class ForbiddenOperationException(string message) : Exception(message)
{
}
