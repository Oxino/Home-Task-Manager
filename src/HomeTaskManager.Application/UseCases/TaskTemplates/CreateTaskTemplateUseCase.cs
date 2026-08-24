using HomeTaskManager.Application.Abstractions;
using HomeTaskManager.Application.Abstractions.Repositories;
using HomeTaskManager.Domain.Entities;
using HomeTaskManager.Domain.Enums;
using HomeTaskManager.Domain.Exceptions;

namespace HomeTaskManager.Application.UseCases.TaskTemplates;

public sealed record CreateTaskTemplateCommand(
    string Title,
    string? Description,
    int ManualDifficultyScore,
    FrequencyType FrequencyType,
    TaskPriority Priority,
    int? EstimatedDurationMinutes,
    Guid CreatedByUserId);

public interface ICreateTaskTemplateUseCase
{
    Task<TaskTemplateDto> ExecuteAsync(CreateTaskTemplateCommand command, CancellationToken cancellationToken = default);
}

public sealed class CreateTaskTemplateUseCase(
    ITaskTemplateRepository taskTemplateRepository,
    IUserRepository userRepository,
    IUnitOfWork unitOfWork) : ICreateTaskTemplateUseCase
{
    public async Task<TaskTemplateDto> ExecuteAsync(CreateTaskTemplateCommand command, CancellationToken cancellationToken = default)
    {
        var author = await userRepository.GetByIdAsync(command.CreatedByUserId, cancellationToken)
            ?? throw new DomainValidationException("The author profile does not exist.");

        if (!author.IsActive)
        {
            throw new DomainValidationException("An inactive profile cannot create tasks.");
        }

        // Title uniqueness (DOMAIN_MODEL: TaskTemplate.Title unique).
        if (await taskTemplateRepository.ExistsByTitleAsync(command.Title, cancellationToken))
        {
            throw new DomainValidationException($"A task named '{command.Title}' already exists.");
        }

        var taskTemplate = new TaskTemplate(
            command.Title,
            command.CreatedByUserId,
            command.ManualDifficultyScore,
            command.FrequencyType,
            command.Priority,
            command.Description,
            command.EstimatedDurationMinutes);

        await taskTemplateRepository.AddAsync(taskTemplate, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return TaskTemplateMapper.ToDto(taskTemplate);
    }
}
