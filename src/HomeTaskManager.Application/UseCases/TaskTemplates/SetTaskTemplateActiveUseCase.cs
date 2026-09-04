using HomeTaskManager.Application.Abstractions;
using HomeTaskManager.Application.Abstractions.Repositories;
using HomeTaskManager.Application.Exceptions;
using HomeTaskManager.Domain.Exceptions;

namespace HomeTaskManager.Application.UseCases.TaskTemplates;

public sealed record SetTaskTemplateActiveCommand(Guid TaskTemplateId, Guid RequestingUserId, bool IsActive);

/// <summary>Admin-only: activates/deactivates a template without deleting it or its already-generated instances (BR-047).</summary>
public interface ISetTaskTemplateActiveUseCase
{
    Task<TaskTemplateDto> ExecuteAsync(SetTaskTemplateActiveCommand command, CancellationToken cancellationToken = default);
}

public sealed class SetTaskTemplateActiveUseCase(
    ITaskTemplateRepository taskTemplateRepository,
    IUserRepository userRepository,
    IAuditLogger auditLogger,
    IUnitOfWork unitOfWork) : ISetTaskTemplateActiveUseCase
{
    public async Task<TaskTemplateDto> ExecuteAsync(SetTaskTemplateActiveCommand command, CancellationToken cancellationToken = default)
    {
        var requestingUser = await userRepository.GetByIdAsync(command.RequestingUserId, cancellationToken)
            ?? throw new DomainValidationException("The requesting profile does not exist.");
        if (!requestingUser.IsAdmin) throw new ForbiddenOperationException("Only an administrator can change a task status.");
        var taskTemplate = await taskTemplateRepository.GetByIdAsync(command.TaskTemplateId, cancellationToken)
            ?? throw new DomainValidationException("The requested task does not exist.");
        taskTemplate.SetActive(command.IsActive);
        await auditLogger.LogAsync(AuditEntityTypes.TaskTemplate, taskTemplate.Id, AuditActions.Updated, requestingUser.Id,
            newValue: $"IsActive={taskTemplate.IsActive}", cancellationToken: cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return TaskTemplateMapper.ToDto(taskTemplate);
    }
}