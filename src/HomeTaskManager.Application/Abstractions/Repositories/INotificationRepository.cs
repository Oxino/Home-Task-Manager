using HomeTaskManager.Domain.Entities;

namespace HomeTaskManager.Application.Abstractions.Repositories;

public interface INotificationRepository
{
    Task<IReadOnlyList<Notification>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<Notification> Items, int TotalCount)> GetPageByUserIdAsync(Guid userId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<Notification?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(Notification notification, CancellationToken cancellationToken = default);
    Task<bool> ExistsByLinkAsync(Guid userId, string link, CancellationToken cancellationToken = default);
    Task DeleteReadByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
}