using HomeTaskManager.Application.Abstractions.Repositories;
using HomeTaskManager.Domain.Entities;
using HomeTaskManager.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace HomeTaskManager.Infrastructure.Persistence.Repositories;

public sealed class WeekRepository(HomeTaskManagerDbContext dbContext) : IWeekRepository
{
    public Task<Week?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.Weeks.SingleOrDefaultAsync(week => week.Id == id, cancellationToken);

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