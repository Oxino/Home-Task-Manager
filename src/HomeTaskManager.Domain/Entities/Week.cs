using HomeTaskManager.Domain.Enums;
using HomeTaskManager.Domain.Exceptions;
using HomeTaskManager.Domain.ValueObjects;

namespace HomeTaskManager.Domain.Entities;

public sealed class Week
{
    private Week()
    {
    }

    public Week(
        WeekKey key,
        int segmentNumber = 1,
        DateOnly? startDate = null,
        DateOnly? endDate = null,
        bool ignoreHistoryForAssignment = false,
        DateTime? startsAt = null,
        DateTime? endsAt = null)
    {
        if (segmentNumber < 1)
        {
            throw new DomainValidationException("The period segment number must be at least one.");
        }

        var weekStartDate = key.StartDate;
        var weekEndDate = weekStartDate.AddDays(6);
        var periodStartDate = startDate ?? weekStartDate;
        var periodEndDate = endDate ?? weekEndDate;
        if (periodStartDate < weekStartDate || periodEndDate > weekEndDate || periodEndDate < periodStartDate)
        {
            throw new DomainValidationException("The period dates must belong to its ISO week and form a valid range.");
        }

        var periodStartsAt = startsAt ?? periodStartDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var periodEndsAt = endsAt ?? periodEndDate.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);
        if (periodStartsAt > periodEndsAt)
        {
            throw new DomainValidationException("The period timestamps must form a valid range.");
        }

        Id = Guid.NewGuid();
        Year = key.Year;
        WeekNumber = key.WeekNumber;
        SegmentNumber = segmentNumber;
        StartDate = periodStartDate;
        EndDate = periodEndDate;
        StartsAt = periodStartsAt;
        EndsAt = periodEndsAt;
        IgnoreHistoryForAssignment = ignoreHistoryForAssignment;
        Status = WeekStatus.Active;
    }

    public Guid Id { get; private set; }

    public int Year { get; private set; }

    public int WeekNumber { get; private set; }

    public int SegmentNumber { get; private set; }

    public DateOnly StartDate { get; private set; }

    public DateOnly EndDate { get; private set; }

    public DateTime StartsAt { get; private set; }

    public DateTime EndsAt { get; private set; }

    public bool IgnoreHistoryForAssignment { get; private set; }

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
        EndsAt = ClosedAt.Value;
    }
}