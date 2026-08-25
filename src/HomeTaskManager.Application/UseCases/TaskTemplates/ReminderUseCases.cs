using HomeTaskManager.Application.Abstractions;
using HomeTaskManager.Application.Abstractions.Repositories;
using HomeTaskManager.Application.Exceptions;
using HomeTaskManager.Domain.Entities;
using HomeTaskManager.Domain.Enums;
using HomeTaskManager.Domain.Exceptions;

namespace HomeTaskManager.Application.UseCases.TaskTemplates;

public sealed record ReminderDto(Guid Id, Guid TaskTemplateId, ReminderTriggerType TriggerType, int OffsetValue, ReminderOffsetUnit OffsetUnit, bool IsEnabled);
public sealed record AddReminderCommand(Guid TaskTemplateId, Guid RequestingUserId, ReminderTriggerType TriggerType, int OffsetValue, ReminderOffsetUnit OffsetUnit);
public sealed record SetReminderEnabledCommand(Guid ReminderId, Guid RequestingUserId, bool IsEnabled);

public interface IReminderUseCase
{
    Task<IReadOnlyList<ReminderDto>> ListAsync(Guid taskTemplateId, Guid requestingUserId, CancellationToken cancellationToken = default);
    Task<ReminderDto> AddAsync(AddReminderCommand command, CancellationToken cancellationToken = default);
    Task<ReminderDto> SetEnabledAsync(SetReminderEnabledCommand command, CancellationToken cancellationToken = default);
}

public sealed class ReminderUseCase(
    IReminderRepository reminderRepository,
    ITaskTemplateRepository taskTemplateRepository,
    IUserRepository userRepository,
    IAuditLogger auditLogger,
    IUnitOfWork unitOfWork) : IReminderUseCase
{
    public async Task<IReadOnlyList<ReminderDto>> ListAsync(Guid taskTemplateId, Guid requestingUserId, CancellationToken cancellationToken = default)
    {
        await EnsureAccessAsync(taskTemplateId, requestingUserId, cancellationToken);
        return (await reminderRepository.GetByTaskTemplateAsync(taskTemplateId, cancellationToken)).Select(ToDto).ToList();
    }

    public async Task<ReminderDto> AddAsync(AddReminderCommand command, CancellationToken cancellationToken = default)
    {
        await EnsureAccessAsync(command.TaskTemplateId, command.RequestingUserId, cancellationToken);
        var reminder = new Reminder(command.TaskTemplateId, command.TriggerType, command.OffsetValue, command.OffsetUnit);
        await reminderRepository.AddAsync(reminder, cancellationToken);
        await auditLogger.LogAsync(AuditEntityTypes.TaskTemplate, command.TaskTemplateId, AuditActions.Updated, command.RequestingUserId,
            newValue: $"ReminderAdded={reminder.Id}; Trigger={reminder.TriggerType}; Offset={reminder.OffsetValue}{reminder.OffsetUnit}", cancellationToken: cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ToDto(reminder);
    }

    public async Task<ReminderDto> SetEnabledAsync(SetReminderEnabledCommand command, CancellationToken cancellationToken = default)
    {
        var reminder = await reminderRepository.GetByIdAsync(command.ReminderId, cancellationToken) ?? throw new DomainValidationException("The requested reminder does not exist.");
        await EnsureAccessAsync(reminder.TaskTemplateId, command.RequestingUserId, cancellationToken);
        var oldEnabled = reminder.IsEnabled;
        reminder.SetEnabled(command.IsEnabled);
        await auditLogger.LogAsync(AuditEntityTypes.TaskTemplate, reminder.TaskTemplateId, AuditActions.Updated, command.RequestingUserId,
            $"ReminderId={reminder.Id}; IsEnabled={oldEnabled}", $"ReminderId={reminder.Id}; IsEnabled={reminder.IsEnabled}", cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ToDto(reminder);
    }

    private async Task EnsureAccessAsync(Guid taskTemplateId, Guid requestingUserId, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(requestingUserId, cancellationToken) ?? throw new DomainValidationException("The requesting profile does not exist.");
        var template = await taskTemplateRepository.GetByIdAsync(taskTemplateId, cancellationToken) ?? throw new DomainValidationException("The requested task does not exist.");
        if (!user.IsAdmin && template.CreatedByUserId != user.Id) throw new ForbiddenOperationException("Only the author or an administrator can manage reminders.");
    }

    private static ReminderDto ToDto(Reminder reminder) => new(reminder.Id, reminder.TaskTemplateId, reminder.TriggerType, reminder.OffsetValue, reminder.OffsetUnit, reminder.IsEnabled);
}