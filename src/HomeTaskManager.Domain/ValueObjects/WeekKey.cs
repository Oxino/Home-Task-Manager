using System.Globalization;
using HomeTaskManager.Domain.Exceptions;

namespace HomeTaskManager.Domain.ValueObjects;

/// <summary>Immutable identifier of an ISO-8601 week (ADR-008): year + week number, validated on construction.</summary>
public readonly record struct WeekKey
{
    public WeekKey(int year, int weekNumber)
    {
        if (year < 1 || year > 9999)
        {
            throw new DomainValidationException("The ISO week year must be between 1 and 9999.");
        }

        // ISO weeks can have 52 or 53 weeks depending on the year; validate against the actual count.
        var weeksInYear = ISOWeek.GetWeeksInYear(year);
        if (weekNumber < 1 || weekNumber > weeksInYear)
        {
            throw new DomainValidationException($"The ISO week number must be between 1 and {weeksInYear} for year {year}.");
        }

        Year = year;
        WeekNumber = weekNumber;
    }

    public int Year { get; }

    public int WeekNumber { get; }

    /// <summary>The Monday that starts this ISO week.</summary>
    public DateOnly StartDate => DateOnly.FromDateTime(ISOWeek.ToDateTime(Year, WeekNumber, DayOfWeek.Monday));

    /// <summary>Computes the ISO week key that contains the given calendar date.</summary>
    public static WeekKey FromDate(DateOnly date)
    {
        var dateTime = date.ToDateTime(TimeOnly.MinValue);
        return new WeekKey(ISOWeek.GetYear(dateTime), ISOWeek.GetWeekOfYear(dateTime));
    }

    /// <summary>The immediately following ISO week (handles year/week-count rollover).</summary>
    public WeekKey Next() => FromDate(StartDate.AddDays(7));

    public override string ToString() => $"{Year}-W{WeekNumber:00}";
}