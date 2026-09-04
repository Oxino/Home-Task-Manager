using HomeTaskManager.Application.Abstractions;
using HomeTaskManager.Application.Abstractions.Repositories;
using HomeTaskManager.Application.Exceptions;
using HomeTaskManager.Domain.Entities;
using HomeTaskManager.Application.UseCases.Weeks;
using HomeTaskManager.Domain.Enums;
using HomeTaskManager.Domain.Exceptions;

namespace HomeTaskManager.Application.UseCases.TaskInstances;

public sealed record ReassignTaskInstanceCommand(Guid TaskInstanceId, Guid RequestingUserId, Guid AssignedUserId);

/// <summary>ADR-013: manually re-points a task instance to a different (active) participant, notifying them.</summary>
public interface IReassignTaskInstanceUseCase
{
    Task<TaskInstanceDto> ExecuteAsync(ReassignTaskInstanceCommand command, CancellationToken cancellationToken = default);
}

public sealed class ReassignTaskInstanceUseCase(
    ITaskInstanceRepository taskInstanceRepository,
    IWeekRepository weekRepository,
    IUserRepository userRepository,
    IAuditLogger auditLogger,
    IUnitOfWork unitOfWork,
    INotificationRepository? notificationRepository = null,
    ITaskTemplateRepository? taskTemplateRepository = null,
    IUserPreferencesRepository? userPreferencesRepository = null,
    INotificationService? notificationService = null) : IReassignTaskInstanceUseCase
{
    public async Task<TaskInstanceDto> ExecuteAsync(ReassignTaskInstanceCommand command, CancellationToken cancellationToken = default)
    {
        var requestingUser = await userRepository.GetByIdAsync(command.RequestingUserId, cancellationToken)
            ?? throw new DomainValidationException("The requesting profile does not exist.");

        var instance = await taskInstanceRepository.GetByIdAsync(command.TaskInstanceId, cancellationToken)
            ?? throw new DomainValidationException("The requested task instance does not exist.");
        if (!requestingUser.IsAdmin && instance.AssignedUserId != requestingUser.Id)
            throw new ForbiddenOperationException("Only the assigned participant or an administrator can reassign a task instance.");
        var week = await weekRepository.GetByIdAsync(instance.WeekId, cancellationToken)
            ?? throw new DomainValidationException("The instance's week does not exist.");
        if (week.Status == WeekStatus.Closed) throw new DomainValidationException("A closed week cannot be reassigned.");

        var assignedUser = await userRepository.GetByIdAsync(command.AssignedUserId, cancellationToken)
            ?? throw new DomainValidationException("The assigned profile does not exist.");
        if (!assignedUser.IsActive) throw new DomainValidationException("An inactive profile cannot receive a task.");

        var previousUserId = instance.AssignedUserId;
        instance.Reassign(command.AssignedUserId);
        // Best-effort notification: silently proceeds without notifying if the optional dependencies weren't wired
        // (e.g. in tests), since notification delivery is not part of this use case's core contract.
        var notificationPreference = userPreferencesRepository is null
            ? "All"
            : (await userPreferencesRepository.GetByUserIdAsync(assignedUser.Id, cancellationToken))?.NotificationPreference ?? "All";
        if (notificationPreference is not "None" && notificationRepository is not null)
        {
            var taskTitle = taskTemplateRepository is null
                ? "une tâche"
                : (await taskTemplateRepository.GetByIdAsync(instance.TaskTemplateId, cancellationToken))?.Title ?? "une tâche";
            var title = "Tâche réaffectée";
            var message = $"La tâche {taskTitle} vous a été réaffectée par {requestingUser.DisplayName}.";
            await notificationRepository.AddAsync(new Notification(
                assignedUser.Id,
                title,
                message,
                $"/week?year={week.Year}&week={week.WeekNumber}&instanceId={instance.Id}",
                NotificationImportance.Important), cancellationToken);
            if (notificationService is not null)
                await notificationService.SendNotificationAsync(assignedUser.Id, title, message, NotificationImportance.Important, cancellationToken);
        }
        await auditLogger.LogAsync(AuditEntityTypes.TaskInstance, instance.Id, AuditActions.Reassigned, requestingUser.Id,
            $"AssignedUserId={previousUserId}", $"AssignedUserId={assignedUser.Id}", cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return WeekMapper.ToDto(instance);
    }
}