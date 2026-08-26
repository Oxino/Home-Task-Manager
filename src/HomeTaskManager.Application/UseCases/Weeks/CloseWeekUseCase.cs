using HomeTaskManager.Application.Abstractions;
using HomeTaskManager.Application.Abstractions.Repositories;
using HomeTaskManager.Application.Exceptions;
using HomeTaskManager.Domain.Entities;
using HomeTaskManager.Domain.Enums;
using HomeTaskManager.Domain.Exceptions;
using HomeTaskManager.Domain.ValueObjects;

namespace HomeTaskManager.Application.UseCases.Weeks;

public sealed record CloseWeekCommand(int Year, int WeekNumber, Guid RequestingUserId);

public sealed record CloseWeekResult(WeekDto Week, IReadOnlyList<TaskInstanceDto> CarriedOverInstances);

public interface ICloseWeekUseCase
{
    Task<CloseWeekResult> ExecuteAsync(CloseWeekCommand command, CancellationToken cancellationToken = default);
}

/// <summary>UC-GEN-02: closes a week (BR-055) and carries unfinished instances onto the next week (ADR-005).</summary>
public sealed class CloseWeekUseCase(
    IWeekRepository weekRepository,
    IUserRepository userRepository,
    ITaskInstanceRepository taskInstanceRepository,
    ITaskTemplateRepository taskTemplateRepository,
    IAuditLogger auditLogger,
    IUnitOfWork unitOfWork) : ICloseWeekUseCase
{
    public async Task<CloseWeekResult> ExecuteAsync(CloseWeekCommand command, CancellationToken cancellationToken = default)
    {
        var requestingUser = await userRepository.GetByIdAsync(command.RequestingUserId, cancellationToken)
            ?? throw new DomainValidationException("The requesting profile does not exist.");

        if (!requestingUser.IsAdmin)
        {
            throw new ForbiddenOperationException("Only an administrator can close a week.");
        }

        var key = new WeekKey(command.Year, command.WeekNumber);
        var week = await weekRepository.GetByKeyAsync(key, cancellationToken)
            ?? throw new DomainValidationException("The week has not been generated yet.");

        var instances = await taskInstanceRepository.GetByWeekAsync(week.Id, cancellationToken);
        var unfinishedInstances = instances
            .Where(instance => !instance.IsCompleted && !instance.IsCarryOver && instance.Status != TaskInstanceStatus.Cancelled)
            .OrderBy(instance => instance.PlannedDate)
            .ThenBy(instance => instance.Id)
            .ToList();
        var templates = (await taskTemplateRepository.GetByIdsAsync(instances.Select(instance => instance.TaskTemplateId).Distinct(), cancellationToken))
            .ToDictionary(template => template.Id);
        var completedFluidTemplates = instances
            .Where(instance => instance.IsCompleted && templates.TryGetValue(instance.TaskTemplateId, out var template) && template.FrequencyType == FrequencyType.WeeklyFlexible)
            .Select(instance => instance.TaskTemplateId)
            .ToHashSet();
        var carryCandidates = unfinishedInstances
            .Where(instance => !completedFluidTemplates.Contains(instance.TaskTemplateId))
            .GroupBy(instance => templates.TryGetValue(instance.TaskTemplateId, out var template) && template.FrequencyType == FrequencyType.WeeklyFlexible ? instance.TaskTemplateId : Guid.Empty)
            .SelectMany(group => group.Key == Guid.Empty ? group : group.Take(1))
            .ToList();
        var completedFluidRemainders = unfinishedInstances
            .Where(instance => completedFluidTemplates.Contains(instance.TaskTemplateId))
            .ToList();
        taskInstanceRepository.RemoveRange(completedFluidRemainders);

        week.Close();

        var carriedOverInstances = new List<TaskInstance>();
        if (carryCandidates.Count > 0)
        {
            var nextKey = key.Next();
            var nextWeek = await weekRepository.GetByKeyAsync(nextKey, cancellationToken);
            if (nextWeek is null)
            {
                nextWeek = new Week(nextKey);
                await weekRepository.AddAsync(nextWeek, cancellationToken);
            }

            var occupiedDates = (await taskInstanceRepository.GetByWeekAsync(nextWeek.Id, cancellationToken))
                .Select(instance => (instance.TaskTemplateId, instance.PlannedDate))
                .ToHashSet();

            foreach (var instance in carryCandidates)
            {
                var plannedDate = nextWeek.StartDate;
                while (!occupiedDates.Add((instance.TaskTemplateId, plannedDate)))
                {
                    plannedDate = plannedDate.AddDays(1);
                }

                carriedOverInstances.Add(new TaskInstance(
                    instance.TaskTemplateId,
                    null,
                    nextWeek,
                    instance.AssignedUserId,
                    plannedDate,
                    plannedDate,
                    isCarryOver: true,
                    parentTaskInstanceId: instance.Id));
            }

            await taskInstanceRepository.AddRangeAsync(carriedOverInstances, cancellationToken);
        }

        await auditLogger.LogAsync(
            AuditEntityTypes.Week, week.Id, AuditActions.Closed, requestingUser.Id,
            newValue: $"CarriedOverCount={carriedOverInstances.Count}", cancellationToken: cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new CloseWeekResult(WeekMapper.ToDto(week), carriedOverInstances.Select(WeekMapper.ToDto).ToList());
    }
}
