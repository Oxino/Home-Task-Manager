using HomeTaskManager.Domain.Enums;
using HomeTaskManager.Domain.Exceptions;
using HomeTaskManager.Domain.ValueObjects;

namespace HomeTaskManager.Domain.Entities;

public sealed class Week
{
    private Week()
    {
    }

    public Week(WeekKey key)
    {
        Id = Guid.NewGuid();
        Year = key.Year;
        WeekNumber = key.WeekNumber;
        StartDate = key.StartDate;
        EndDate = key.StartDate.AddDays(6);
        Status = WeekStatus.Active;
    }

    public Guid Id { get; private set; }

    public int Year { get; private set; }

    public int WeekNumber { get; private set; }

    public DateOnly StartDate { get; private set; }

    public DateOnly EndDate { get; private set; }

    public WeekStatus Status { get; private set; }

    public DateTime? ClosedAt { get; private set; }

    public void Close(DateTime? closedAt = null)
    {
        if (Status == WeekStatus.Closed)
        {
            throw new DomainValidationException("A closed week cannot be closed again.");
        }

        Status = WeekStatus.Closed;
        ClosedAt = closedAt ?? DateTime.UtcNow;
    }
}