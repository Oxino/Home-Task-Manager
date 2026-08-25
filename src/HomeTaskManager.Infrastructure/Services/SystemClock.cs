using HomeTaskManager.Application.Abstractions;

namespace HomeTaskManager.Infrastructure.Services;

public sealed class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}
