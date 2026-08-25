using HomeTaskManager.Application.Abstractions;
using HomeTaskManager.Application.Abstractions.Repositories;
using HomeTaskManager.Application.Exceptions;
using HomeTaskManager.Application.UseCases.Weeks;
using HomeTaskManager.Domain.Enums;
using HomeTaskManager.Domain.Exceptions;

namespace HomeTaskManager.Application.UseCases.TaskInstances;

public sealed record ReassignTaskInstanceCommand(Guid TaskInstanceId, Guid RequestingUserId, Guid AssignedUserId);

public interface IReassignTaskInstanceUseCase
{
    Task<TaskInstanceDto> ExecuteAsync(ReassignTaskInstanceCommand command, CancellationToken cancellationToken = default);
}

public sealed class ReassignTaskInstanceUseCase(
    ITaskInstanceRepository taskInstanceRepository,
    IWeekRepository weekRepository,
    IUserRepository userRepository,
    IAuditLogger auditLogger,
    IUnitOfWork unitOfWork) : IReassignTaskInstanceUseCase
{
    public async Task<TaskInstanceDto> ExecuteAsync(ReassignTaskInstanceCommand command, CancellationToken cancellationToken = default)
    {
        var requestingUser = await userRepository.GetByIdAsync(command.RequestingUserId, cancellationToken)
            ?? throw new DomainValidationException("The requesting profile does not exist.");
        if (!requestingUser.IsAdmin) throw new ForbiddenOperationException("Only an administrator can reassign a task instance.");

        var instance = await taskInstanceRepository.GetByIdAsync(command.TaskInstanceId, cancellationToken)
            ?? throw new DomainValidationException("The requested task instance does not exist.");
        var week = await weekRepository.GetByIdAsync(instance.WeekId, cancellationToken)
            ?? throw new DomainValidationException("The instance's week does not exist.");
        if (week.Status == WeekStatus.Closed) throw new DomainValidationException("A closed week cannot be reassigned.");

        var assignedUser = await userRepository.GetByIdAsync(command.AssignedUserId, cancellationToken)
            ?? throw new DomainValidationException("The assigned profile does not exist.");
        if (!assignedUser.IsActive) throw new DomainValidationException("An inactive profile cannot receive a task.");

        var previousUserId = instance.AssignedUserId;
        instance.Reassign(command.AssignedUserId);
        await auditLogger.LogAsync(AuditEntityTypes.TaskInstance, instance.Id, AuditActions.Reassigned, requestingUser.Id,
            $"AssignedUserId={previousUserId}", $"AssignedUserId={assignedUser.Id}", cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return WeekMapper.ToDto(instance);
    }
}