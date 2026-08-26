using HomeTaskManager.Application.Abstractions;
using HomeTaskManager.Application.Abstractions.Repositories;
using HomeTaskManager.Application.Exceptions;
using HomeTaskManager.Domain.Entities;
using HomeTaskManager.Domain.Exceptions;

namespace HomeTaskManager.Application.UseCases.Users;

public sealed record NotificationDto(Guid Id, Guid UserId, string Title, string Message, string? Link, bool IsRead, DateTime? ReadAt, DateTime CreatedAt);
public sealed record SetNotificationReadCommand(Guid NotificationId, Guid UserId, bool IsRead);

public interface INotificationUseCase
{
    Task<IReadOnlyList<NotificationDto>> ListAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<PagedResult<NotificationDto>> ListPageAsync(Guid userId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<NotificationDto> SetReadAsync(SetNotificationReadCommand command, CancellationToken cancellationToken = default);
    Task MarkAllReadAsync(Guid userId, CancellationToken cancellationToken = default);
    Task DeleteReadAsync(Guid userId, CancellationToken cancellationToken = default);
}

public sealed class NotificationUseCase(INotificationRepository repository, IUnitOfWork unitOfWork) : INotificationUseCase
{
    public async Task<IReadOnlyList<NotificationDto>> ListAsync(Guid userId, CancellationToken cancellationToken = default) =>
        (await repository.GetByUserIdAsync(userId, cancellationToken)).Select(ToDto).ToList();

    public async Task<PagedResult<NotificationDto>> ListPageAsync(Guid userId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var result = await repository.GetPageByUserIdAsync(userId, page, pageSize, cancellationToken);
        return new PagedResult<NotificationDto>(result.Items.Select(ToDto).ToList(), page, pageSize, result.TotalCount);
    }

    public async Task<NotificationDto> SetReadAsync(SetNotificationReadCommand command, CancellationToken cancellationToken = default)
    {
        var notification = await repository.GetByIdAsync(command.NotificationId, cancellationToken)
            ?? throw new DomainValidationException("The requested notification does not exist.");
        if (notification.UserId != command.UserId) throw new ForbiddenOperationException("This notification belongs to another profile.");
        if (command.IsRead) notification.MarkAsRead();
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ToDto(notification);
    }

    public async Task MarkAllReadAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        foreach (var notification in await repository.GetByUserIdAsync(userId, cancellationToken)) notification.MarkAsRead();
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteReadAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        await repository.DeleteReadByUserIdAsync(userId, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static NotificationDto ToDto(Notification notification) => new(notification.Id, notification.UserId, notification.Title, notification.Message, notification.Link, notification.IsRead, notification.ReadAt, notification.CreatedAt);
}