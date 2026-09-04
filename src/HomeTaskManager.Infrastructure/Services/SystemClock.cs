using HomeTaskManager.Application.Abstractions;

namespace HomeTaskManager.Infrastructure.Services;

/// <summary>Production <see cref="IClock"/> backed by the real system clock; tests substitute a fake instead.</summary>
public sealed class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}
