using HomeTaskManager.Application.Abstractions.Repositories;
using HomeTaskManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HomeTaskManager.Infrastructure.Persistence.Repositories;

/// <summary>EF Core implementation of <see cref="ISystemSettingRepository"/>.</summary>
public sealed class SystemSettingRepository(HomeTaskManagerDbContext dbContext) : ISystemSettingRepository
{
    public Task<SystemSetting?> GetByKeyAsync(string key, CancellationToken cancellationToken = default) =>
        dbContext.SystemSettings.SingleOrDefaultAsync(setting => setting.Key == key, cancellationToken);

    public async Task<IReadOnlyList<SystemSetting>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await dbContext.SystemSettings.ToListAsync(cancellationToken);

    public Task AddAsync(SystemSetting setting, CancellationToken cancellationToken = default)
    {
        return dbContext.SystemSettings.AddAsync(setting, cancellationToken).AsTask();
    }
}
