using HomeTaskManager.Application.Abstractions.Repositories;
using HomeTaskManager.Domain.Entities;
using HomeTaskManager.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace HomeTaskManager.Infrastructure.Persistence.Repositories;

/// <summary>EF Core implementation of <see cref="IWeekRepository"/>.</summary>
public sealed class WeekRepository(HomeTaskManagerDbContext dbContext) : IWeekRepository
{
    public Task<Week?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.Weeks.SingleOrDefaultAsync(week => week.Id == id, cancellationToken);

    // A given ISO week key can have several segments (mid-week closures); prefer the still-Active one,
    // and among equally-active/closed candidates take the highest segment number (the most recent one).
    public Task<Week?> GetByKeyAsync(WeekKey key, CancellationToken cancellationToken = default) =>
        dbContext.Weeks.Where(
            week => week.Year == key.Year && week.WeekNumber == key.WeekNumber)
            .OrderByDescending(week => week.Status == Domain.Enums.WeekStatus.Active)
            .ThenByDescending(week => week.SegmentNumber)
            .FirstOrDefaultAsync(
            cancellationToken);

    public async Task<int> GetNextSegmentNumberAsync(WeekKey key, CancellationToken cancellationToken = default) =>
        (await dbContext.Weeks
            .Where(week => week.Year == key.Year && week.WeekNumber == key.WeekNumber)
            .MaxAsync(week => (int?)week.SegmentNumber, cancellationToken) ?? 0) + 1;

    public Task AddAsync(Week week, CancellationToken cancellationToken = default)
    {
        return dbContext.Weeks.AddAsync(week, cancellationToken).AsTask();
    }
}