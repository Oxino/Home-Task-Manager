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
    Guid CreatedByUserId,
    IReadOnlyCollection<DayOfWeek>? ActiveWeekDays = null,
    MonthlyScheduleType MonthlyScheduleType = MonthlyScheduleType.StartOfMonth,
    int? MonthlyDay = null,
    WeeklyScheduleType WeeklyScheduleType = WeeklyScheduleType.EndOfWeek,
    bool DistributeDailyMandatory = true);

public interface ICreateTaskTemplateUseCase
{
    Task<TaskTemplateDto> ExecuteAsync(CreateTaskTemplateCommand command, CancellationToken cancellationToken = default);
}

public sealed class CreateTaskTemplateUseCase(
    ITaskTemplateRepository taskTemplateRepository,
    IUserRepository userRepository,
    IAuditLogger auditLogger,
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
            command.EstimatedDurationMinutes,
            command.ActiveWeekDays,
            command.MonthlyScheduleType,
            command.MonthlyDay,
            command.WeeklyScheduleType,
            command.DistributeDailyMandatory);

        await taskTemplateRepository.AddAsync(taskTemplate, cancellationToken);

        await auditLogger.LogAsync(
            AuditEntityTypes.TaskTemplate, taskTemplate.Id, AuditActions.Created, author.Id,
            newValue: $"Title={taskTemplate.Title}", cancellationToken: cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return TaskTemplateMapper.ToDto(taskTemplate);
    }
}
