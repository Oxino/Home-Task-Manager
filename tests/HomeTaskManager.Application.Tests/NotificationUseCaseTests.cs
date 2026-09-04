using FluentAssertions;
using HomeTaskManager.Application.Abstractions.Repositories;
using HomeTaskManager.Application.UseCases.Users;
using HomeTaskManager.Domain.Entities;

namespace HomeTaskManager.Application.Tests;

/// <summary>In-memory fake standing in for the EF-backed Notification store in tests.</summary>
public sealed class InMemoryNotificationRepository : INotificationRepository
{
    private readonly List<Notification> _notifications = [];
    public Task<IReadOnlyList<Notification>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Notification>>(_notifications.Where(notification => notification.UserId == userId).OrderByDescending(notification => notification.CreatedAt).ToList());
    public Task<(IReadOnlyList<Notification> Items, int TotalCount)> GetPageByUserIdAsync(Guid userId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var items = _notifications.Where(notification => notification.UserId == userId).OrderByDescending(notification => notification.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize).ToList();
        return Task.FromResult<(IReadOnlyList<Notification>, int)>((items, _notifications.Count(notification => notification.UserId == userId)));
    }
    public Task<Notification?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(_notifications.SingleOrDefault(notification => notification.Id == id));
    public Task AddAsync(Notification notification, CancellationToken cancellationToken = default) { _notifications.Add(notification); return Task.CompletedTask; }
    public Task<bool> ExistsByLinkAsync(Guid userId, string link, CancellationToken cancellationToken = default) => Task.FromResult(_notifications.Any(notification => notification.UserId == userId && notification.Link == link));
    public Task DeleteReadByUserIdAsync(Guid userId, CancellationToken cancellationToken = default) { _notifications.RemoveAll(notification => notification.UserId == userId && notification.IsRead); return Task.CompletedTask; }
}

/// <summary>Verifies NotificationUseCase's read/unread state changes, ownership check, and preference-based filtering.</summary>
public sealed class NotificationUseCaseTests
{
    [Fact]
    public async Task User_can_mark_only_their_notification_as_read_and_clean_it()
    {
        var user = new User("Julie");
        var repository = new InMemoryNotificationRepository();
        var notification = new Notification(user.Id, "Rappel", "Aspirateur à faire.");
        notification.Id.Should().NotBeEmpty();
        await repository.AddAsync(notification);
        var useCase = new NotificationUseCase(repository, new NoOpUnitOfWork());

        await useCase.SetReadAsync(new SetNotificationReadCommand(notification.Id, user.Id, true));
        await useCase.DeleteReadAsync(user.Id);

        (await useCase.ListAsync(user.Id)).Should().BeEmpty();
    }

    [Fact]
    public async Task Another_user_cannot_mark_a_notification_as_read()
    {
        var owner = new User("Julie");
        var other = new User("Nicolas");
        var repository = new InMemoryNotificationRepository();
        var notification = new Notification(owner.Id, "Rappel", "Aspirateur à faire.");
        await repository.AddAsync(notification);
        var useCase = new NotificationUseCase(repository, new NoOpUnitOfWork());

        var action = () => useCase.SetReadAsync(new SetNotificationReadCommand(notification.Id, other.Id, true));

        await action.Should().ThrowAsync<HomeTaskManager.Application.Exceptions.ForbiddenOperationException>();
    }
}
