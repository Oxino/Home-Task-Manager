using HomeTaskManager.Application.Abstractions;
using HomeTaskManager.Application.Abstractions.Repositories;
using HomeTaskManager.Application.Exceptions;
using HomeTaskManager.Domain.Enums;
using HomeTaskManager.Domain.Exceptions;

namespace HomeTaskManager.Application.UseCases.TaskTemplates;

public sealed record UpdateTaskTemplateCommand(
    Guid TaskTemplateId,
    Guid RequestingUserId,
    string Title,
    string? Description,
    int ManualDifficultyScore,
    TaskPriority Priority,
    FrequencyType FrequencyType,
    int? EstimatedDurationMinutes);

public interface IUpdateTaskTemplateUseCase
{
    Task<TaskTemplateDto> ExecuteAsync(UpdateTaskTemplateCommand command, CancellationToken cancellationToken = default);
}

public sealed class UpdateTaskTemplateUseCase(
    ITaskTemplateRepository taskTemplateRepository,
    IUserRepository userRepository,
    IAuditLogger auditLogger,
    IUnitOfWork unitOfWork) : IUpdateTaskTemplateUseCase
{
    public async Task<TaskTemplateDto> ExecuteAsync(UpdateTaskTemplateCommand command, CancellationToken cancellationToken = default)
    {
        var requestingUser = await userRepository.GetByIdAsync(command.RequestingUserId, cancellationToken)
            ?? throw new DomainValidationException("The requesting profile does not exist.");

        var taskTemplate = await taskTemplateRepository.GetByIdAsync(command.TaskTemplateId, cancellationToken)
            ?? throw new DomainValidationException("The requested task does not exist.");

        // BR-037 / ADR-007: the administrator can modify any task, an author only their own.
        if (!requestingUser.IsAdmin && taskTemplate.CreatedByUserId != requestingUser.Id)
        {
            throw new ForbiddenOperationException("Only the author or an administrator can modify this task.");
        }

        var oldValue = $"Title={taskTemplate.Title}; Priority={taskTemplate.Priority}";

        taskTemplate.Rename(command.Title);
        taskTemplate.ChangeDescription(command.Description);
        taskTemplate.ChangeDifficulty(command.ManualDifficultyScore);
        taskTemplate.ChangePriority(command.Priority);
        taskTemplate.ChangeFrequency(command.FrequencyType);
        taskTemplate.ChangeEstimatedDuration(command.EstimatedDurationMinutes);

        await auditLogger.LogAsync(
            AuditEntityTypes.TaskTemplate, taskTemplate.Id, AuditActions.Updated, requestingUser.Id,
            oldValue, $"Title={taskTemplate.Title}; Priority={taskTemplate.Priority}", cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return TaskTemplateMapper.ToDto(taskTemplate);
    }
}
