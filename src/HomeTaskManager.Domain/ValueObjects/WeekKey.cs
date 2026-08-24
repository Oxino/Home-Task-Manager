using System.Globalization;
using HomeTaskManager.Domain.Exceptions;

namespace HomeTaskManager.Domain.ValueObjects;

public readonly record struct WeekKey
{
    public WeekKey(int year, int weekNumber)
    {
        if (year < 1 || year > 9999)
        {
            throw new DomainValidationException("The ISO week year must be between 1 and 9999.");
        }

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

    public DateOnly StartDate => DateOnly.FromDateTime(ISOWeek.ToDateTime(Year, WeekNumber, DayOfWeek.Monday));

    public static WeekKey FromDate(DateOnly date)
    {
        var dateTime = date.ToDateTime(TimeOnly.MinValue);
        return new WeekKey(ISOWeek.GetYear(dateTime), ISOWeek.GetWeekOfYear(dateTime));
    }

    public WeekKey Next() => FromDate(StartDate.AddDays(7));

    public override string ToString() => $"{Year}-W{WeekNumber:00}";
}