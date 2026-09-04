using HomeTaskManager.Application.Abstractions;
using HomeTaskManager.Application.Abstractions.Repositories;
using HomeTaskManager.Application.Exceptions;
using HomeTaskManager.Application.UseCases.Weeks;
using HomeTaskManager.Domain.Entities;
using HomeTaskManager.Domain.Enums;
using HomeTaskManager.Domain.Exceptions;

namespace HomeTaskManager.Application.UseCases.TaskInstances;

public sealed record DailyFlexibleExecutionDto(Guid Id, Guid TaskInstanceId, Guid ExecutedByUserId, DateOnly ExecutionDate, DateTime CreatedAt);
public sealed record ExecuteDailyFlexibleTaskCommand(Guid TaskInstanceId, Guid RequestingUserId, DateOnly ExecutionDate);

/// <summary>Logs one execution of a DailyFlexible task (done on a given day, any number of times per week).</summary>
public interface IExecuteDailyFlexibleTaskUseCase
{
    Task<DailyFlexibleExecutionDto> ExecuteAsync(ExecuteDailyFlexibleTaskCommand command, CancellationToken cancellationToken = default);
}

public sealed class ExecuteDailyFlexibleTaskUseCase(
    IDailyFlexibleExecutionRepository executionRepository,
    ITaskInstanceRepository instanceRepository,
    ITaskTemplateRepository templateRepository,
    IWeekRepository weekRepository,
    IUserRepository userRepository,
    IUnitOfWork unitOfWork) : IExecuteDailyFlexibleTaskUseCase
{
    public async Task<DailyFlexibleExecutionDto> ExecuteAsync(ExecuteDailyFlexibleTaskCommand command, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByIdAsync(command.RequestingUserId, cancellationToken) ?? throw new DomainValidationException("The requesting profile does not exist.");
        var instance = await instanceRepository.GetByIdAsync(command.TaskInstanceId, cancellationToken) ?? throw new DomainValidationException("The requested task instance does not exist.");
        if (!user.IsAdmin && instance.AssignedUserId != user.Id) throw new ForbiddenOperationException("Only the assigned participant can execute this task.");
        var week = await weekRepository.GetByIdAsync(instance.WeekId, cancellationToken) ?? throw new DomainValidationException("The instance's week does not exist.");
        if (week.Status == WeekStatus.Closed) throw new DomainValidationException("A closed week cannot be executed.");
        if (instance.Status == TaskInstanceStatus.Cancelled) throw new DomainValidationException("A cancelled task cannot be executed.");
        var template = await templateRepository.GetByIdAsync(instance.TaskTemplateId, cancellationToken) ?? throw new DomainValidationException("The task template does not exist.");
        if (template.FrequencyType != FrequencyType.DailyFlexible) throw new DomainValidationException("Only flexible daily tasks can record multiple executions.");

        var execution = new DailyFlexibleExecution(instance.Id, user.Id, command.ExecutionDate, week.StartDate, week.EndDate);
        await executionRepository.AddAsync(execution, cancellationToken);
        if (!instance.IsCompleted)
        {
            // First execution of the week completes the tracked instance and, if days remain in the week,
            // spawns a fresh placeholder instance for the next day so the task keeps appearing as available.
            instance.SetCompletion(true, week);
            var nextDate = command.ExecutionDate.AddDays(1);
            if (nextDate <= week.EndDate)
            {
                await instanceRepository.AddRangeAsync([
                    new TaskInstance(template.Id, null, week, instance.AssignedUserId, nextDate, nextDate)
                ], cancellationToken);
            }
        }
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new DailyFlexibleExecutionDto(execution.Id, execution.TaskInstanceId, execution.ExecutedByUserId, execution.ExecutionDate, execution.CreatedAt);
    }
}