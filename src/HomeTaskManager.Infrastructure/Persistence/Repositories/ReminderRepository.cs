using HomeTaskManager.Application.Abstractions.Repositories;
using HomeTaskManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HomeTaskManager.Infrastructure.Persistence.Repositories;

public sealed class ReminderRepository(HomeTaskManagerDbContext dbContext) : IReminderRepository
{
    public Task<Reminder?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => dbContext.Reminders.SingleOrDefaultAsync(reminder => reminder.Id == id, cancellationToken);
    public async Task<IReadOnlyList<Reminder>> GetByTaskTemplateAsync(Guid taskTemplateId, CancellationToken cancellationToken = default) => await dbContext.Reminders.Where(reminder => reminder.TaskTemplateId == taskTemplateId).ToListAsync(cancellationToken);
    public async Task<IReadOnlyList<Reminder>> GetEnabledAsync(CancellationToken cancellationToken = default) => await dbContext.Reminders.Where(reminder => reminder.IsEnabled).ToListAsync(cancellationToken);
    public Task AddAsync(Reminder reminder, CancellationToken cancellationToken = default) => dbContext.Reminders.AddAsync(reminder, cancellationToken).AsTask();
}