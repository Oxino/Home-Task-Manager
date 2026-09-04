using HomeTaskManager.Application.Abstractions.Repositories;
using HomeTaskManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HomeTaskManager.Infrastructure.Persistence.Repositories;

/// <summary>EF Core implementation of <see cref="INotificationRepository"/>.</summary>
public sealed class NotificationRepository(HomeTaskManagerDbContext dbContext) : INotificationRepository
{
    public async Task<IReadOnlyList<Notification>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default) => await dbContext.Notifications.Where(notification => notification.UserId == userId).OrderByDescending(notification => notification.CreatedAt).ToListAsync(cancellationToken);
    public async Task<(IReadOnlyList<Notification> Items, int TotalCount)> GetPageByUserIdAsync(Guid userId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Notifications.Where(notification => notification.UserId == userId);
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(notification => notification.CreatedAt).ThenByDescending(notification => notification.Id)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return (items, totalCount);
    }
    public Task<Notification?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => dbContext.Notifications.SingleOrDefaultAsync(notification => notification.Id == id, cancellationToken);
    public Task AddAsync(Notification notification, CancellationToken cancellationToken = default) => dbContext.Notifications.AddAsync(notification, cancellationToken).AsTask();
    public Task<bool> ExistsByLinkAsync(Guid userId, string link, CancellationToken cancellationToken = default) => dbContext.Notifications.AnyAsync(notification => notification.UserId == userId && notification.Link == link, cancellationToken);
    // Bulk SQL delete (no entity materialization/tracking) since read notifications can accumulate and are never needed again.
    public async Task DeleteReadByUserIdAsync(Guid userId, CancellationToken cancellationToken = default) => await dbContext.Notifications.Where(notification => notification.UserId == userId && notification.IsRead).ExecuteDeleteAsync(cancellationToken);
}