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
        dbContext.Weeks.SingleOrDefaultAsync(
            week => week.Year == key.Year && week.WeekNumber == key.WeekNumber,
            cancellationToken);

    public Task AddAsync(Week week, CancellationToken cancellationToken = default)
    {
        return dbContext.Weeks.AddAsync(week, cancellationToken).AsTask();
    }
}