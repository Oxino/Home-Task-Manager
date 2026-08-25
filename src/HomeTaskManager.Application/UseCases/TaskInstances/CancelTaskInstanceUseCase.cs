using HomeTaskManager.Application.Abstractions;
using HomeTaskManager.Application.Abstractions.Repositories;
using HomeTaskManager.Application.Exceptions;
using HomeTaskManager.Application.UseCases.Weeks;
using HomeTaskManager.Domain.Enums;
using HomeTaskManager.Domain.Exceptions;

namespace HomeTaskManager.Application.UseCases.TaskInstances;

public sealed record CancelTaskInstanceCommand(Guid TaskInstanceId, Guid RequestingUserId);

public interface ICancelTaskInstanceUseCase
{
    Task<TaskInstanceDto> ExecuteAsync(CancelTaskInstanceCommand command, CancellationToken cancellationToken = default);
}

public sealed class CancelTaskInstanceUseCase(
    ITaskInstanceRepository taskInstanceRepository,
    IWeekRepository weekRepository,
    IUserRepository userRepository,
    IAuditLogger auditLogger,
    IUnitOfWork unitOfWork) : ICancelTaskInstanceUseCase
{
    public async Task<TaskInstanceDto> ExecuteAsync(CancelTaskInstanceCommand command, CancellationToken cancellationToken = default)
    {
        var requestingUser = await userRepository.GetByIdAsync(command.RequestingUserId, cancellationToken)
            ?? throw new DomainValidationException("The requesting profile does not exist.");
        if (!requestingUser.IsAdmin) throw new ForbiddenOperationException("Only an administrator can cancel a task instance.");

        var instance = await taskInstanceRepository.GetByIdAsync(command.TaskInstanceId, cancellationToken)
            ?? throw new DomainValidationException("The requested task instance does not exist.");
        var week = await weekRepository.GetByIdAsync(instance.WeekId, cancellationToken)
            ?? throw new DomainValidationException("The instance's week does not exist.");
        if (week.Status == WeekStatus.Closed) throw new DomainValidationException("A closed week cannot be changed.");

        instance.Cancel();
        await auditLogger.LogAsync(AuditEntityTypes.TaskInstance, instance.Id, AuditActions.Cancelled, requestingUser.Id,
            newValue: "Status=Cancelled", cancellationToken: cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return WeekMapper.ToDto(instance);
    }
}