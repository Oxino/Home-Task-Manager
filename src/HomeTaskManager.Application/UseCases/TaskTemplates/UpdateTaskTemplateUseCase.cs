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
    int? EstimatedDurationMinutes,
    IReadOnlyCollection<DayOfWeek>? ActiveWeekDays = null,
    MonthlyScheduleType MonthlyScheduleType = MonthlyScheduleType.StartOfMonth,
    int? MonthlyDay = null,
    WeeklyScheduleType WeeklyScheduleType = WeeklyScheduleType.EndOfWeek,
    bool DistributeDailyMandatory = true);

/// <summary>Edits an existing task template's definition; never retroactively changes instances already generated (BR-047).</summary>
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

        if (!string.Equals(taskTemplate.Title, command.Title, StringComparison.OrdinalIgnoreCase) &&
            await taskTemplateRepository.ExistsByTitleAsync(command.Title, taskTemplate.Id, cancellationToken))
        {
            throw new DomainValidationException($"A task named '{command.Title}' already exists.");
        }

        var oldValue = $"Title={taskTemplate.Title}; Priority={taskTemplate.Priority}";

        // Apply every field change through the entity's own setters so domain invariants are re-validated.
        taskTemplate.Rename(command.Title);
        taskTemplate.ChangeDescription(command.Description);
        taskTemplate.ChangeDifficulty(command.ManualDifficultyScore);
        taskTemplate.ChangePriority(command.Priority);
        taskTemplate.ChangeFrequency(command.FrequencyType);
        taskTemplate.SetActiveWeekDays(command.ActiveWeekDays ?? []);
        taskTemplate.SetMonthlySchedule(command.MonthlyScheduleType, command.MonthlyDay);
        taskTemplate.SetWeeklySchedule(command.WeeklyScheduleType);
        taskTemplate.SetDistributeDailyMandatory(command.DistributeDailyMandatory);
        taskTemplate.ChangeEstimatedDuration(command.EstimatedDurationMinutes);

        await auditLogger.LogAsync(
            AuditEntityTypes.TaskTemplate, taskTemplate.Id, AuditActions.Updated, requestingUser.Id,
            oldValue, $"Title={taskTemplate.Title}; Priority={taskTemplate.Priority}", cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return TaskTemplateMapper.ToDto(taskTemplate);
    }
}
