using HomeTaskManager.Application.Abstractions;
using HomeTaskManager.Application.Abstractions.Repositories;
using HomeTaskManager.Application.Exceptions;
using HomeTaskManager.Application.UseCases.Weeks;
using HomeTaskManager.Domain.Exceptions;

namespace HomeTaskManager.Application.UseCases.TaskInstances;

public sealed record SetTaskInstanceCompletionCommand(Guid TaskInstanceId, Guid RequestingUserId, bool IsCompleted);

public interface ISetTaskInstanceCompletionUseCase
{
    Task<TaskInstanceDto> ExecuteAsync(SetTaskInstanceCompletionCommand command, CancellationToken cancellationToken = default);
}

/// <summary>UC-CMP-01/02: the owning participant toggles completion; locked once the week is closed (BR-043/BR-055).</summary>
public sealed class SetTaskInstanceCompletionUseCase(
    ITaskInstanceRepository taskInstanceRepository,
    IWeekRepository weekRepository,
    IUserRepository userRepository,
    IUnitOfWork unitOfWork) : ISetTaskInstanceCompletionUseCase
{
    public async Task<TaskInstanceDto> ExecuteAsync(
        SetTaskInstanceCompletionCommand command,
        CancellationToken cancellationToken = default)
    {
        var requestingUser = await userRepository.GetByIdAsync(command.RequestingUserId, cancellationToken)
            ?? throw new DomainValidationException("The requesting profile does not exist.");

        var instance = await taskInstanceRepository.GetByIdAsync(command.TaskInstanceId, cancellationToken)
            ?? throw new DomainValidationException("The requested task instance does not exist.");

        // UC-CMP-01/02: only the assigned participant may change their own completion state.
        if (instance.AssignedUserId != requestingUser.Id)
        {
            throw new ForbiddenOperationException("Only the assigned participant can update this task's completion.");
        }

        var week = await weekRepository.GetByIdAsync(instance.WeekId, cancellationToken)
            ?? throw new DomainValidationException("The instance's week does not exist.");

        instance.SetCompletion(command.IsCompleted, week);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return WeekMapper.ToDto(instance);
    }
}
