using HomeTaskManager.Application.Abstractions;
using HomeTaskManager.Application.Abstractions.Repositories;
using HomeTaskManager.Application.Exceptions;
using HomeTaskManager.Domain.Entities;
using HomeTaskManager.Domain.Enums;
using HomeTaskManager.Domain.Exceptions;
using HomeTaskManager.Domain.ValueObjects;

namespace HomeTaskManager.Application.UseCases.Weeks;

public sealed record CloseWeekCommand(int Year, int WeekNumber, Guid RequestingUserId, bool CancelIncompleteTasks = false, DateTime? ClosedAt = null);

public sealed record CloseWeekResult(WeekDto Week, IReadOnlyList<TaskInstanceDto> CarriedOverInstances, WeekDto? NextPeriod = null);

public interface ICloseWeekUseCase
{
    Task<CloseWeekResult> ExecuteAsync(CloseWeekCommand command, CancellationToken cancellationToken = default);
}

public sealed record RecoverWeekContinuationCommand(int Year, int WeekNumber, Guid RequestingUserId);

public interface IRecoverWeekContinuationUseCase
{
    Task<WeekDto> ExecuteAsync(RecoverWeekContinuationCommand command, CancellationToken cancellationToken = default);
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
            .Where(instance => !instance.IsCompleted && instance.Status != TaskInstanceStatus.Cancelled)
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
            .Where(instance => !instance.IsCarryOver)
            .Where(instance => !completedFluidTemplates.Contains(instance.TaskTemplateId))
            .GroupBy(instance => templates.TryGetValue(instance.TaskTemplateId, out var template) && template.FrequencyType == FrequencyType.WeeklyFlexible ? instance.TaskTemplateId : Guid.Empty)
            .SelectMany(group => group.Key == Guid.Empty ? group : group.Take(1))
            .ToList();
        var completedFluidRemainders = unfinishedInstances
            .Where(instance => completedFluidTemplates.Contains(instance.TaskTemplateId))
            .ToList();
        taskInstanceRepository.RemoveRange(completedFluidRemainders);

        if (command.CancelIncompleteTasks)
        {
            foreach (var instance in unfinishedInstances)
            {
                instance.Cancel();
            }
            carryCandidates.Clear();
        }

        var closedAt = command.ClosedAt ?? DateTime.UtcNow;
        week.Close(closedAt);

        var carriedOverInstances = new List<TaskInstance>();
        var fullWeek = new WeekKey(command.Year, command.WeekNumber);
        var closedOn = DateOnly.FromDateTime(closedAt);
        var isEarlyClosure = closedOn >= week.StartDate && closedOn < week.EndDate;
        Week? nextPeriod = null;
        if (isEarlyClosure)
        {
            nextPeriod = new Week(
                fullWeek,
                await weekRepository.GetNextSegmentNumberAsync(fullWeek, cancellationToken),
                closedOn,
                week.EndDate,
                command.CancelIncompleteTasks,
                closedAt,
                week.EndsAt);
            await weekRepository.AddAsync(nextPeriod, cancellationToken);
        }
        else if (carryCandidates.Count > 0)
        {
            var nextKey = key.Next();
            nextPeriod = await weekRepository.GetByKeyAsync(nextKey, cancellationToken);
            if (nextPeriod is null)
            {
                nextPeriod = new Week(nextKey);
                await weekRepository.AddAsync(nextPeriod, cancellationToken);
            }
        }

        if (nextPeriod is not null && carryCandidates.Count > 0)
        {
            var occupiedDates = (await taskInstanceRepository.GetByWeekAsync(nextPeriod.Id, cancellationToken))
                .Select(instance => (instance.TaskTemplateId, instance.PlannedDate))
                .ToHashSet();

            foreach (var instance in carryCandidates)
            {
                var plannedDate = nextPeriod.StartDate;
                while (!occupiedDates.Add((instance.TaskTemplateId, plannedDate)))
                {
                    plannedDate = plannedDate.AddDays(1);
                }

                carriedOverInstances.Add(new TaskInstance(
                    instance.TaskTemplateId,
                    null,
                    nextPeriod,
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
            newValue: $"CarriedOverCount={carriedOverInstances.Count}; CancelIncompleteTasks={command.CancelIncompleteTasks}; NextPeriodId={nextPeriod?.Id}", cancellationToken: cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new CloseWeekResult(WeekMapper.ToDto(week), carriedOverInstances.Select(WeekMapper.ToDto).ToList(), nextPeriod is null ? null : WeekMapper.ToDto(nextPeriod));
    }
}

/// <summary>Reconstructs a remaining in-week period for a week closed before period segmentation existed.</summary>
public sealed class RecoverWeekContinuationUseCase(
    IWeekRepository weekRepository,
    IUserRepository userRepository,
    ITaskInstanceRepository taskInstanceRepository,
    IAuditLogger auditLogger,
    IUnitOfWork unitOfWork) : IRecoverWeekContinuationUseCase
{
    public async Task<WeekDto> ExecuteAsync(
        RecoverWeekContinuationCommand command,
        CancellationToken cancellationToken = default)
    {
        var requestingUser = await userRepository.GetByIdAsync(command.RequestingUserId, cancellationToken)
            ?? throw new DomainValidationException("The requesting profile does not exist.");
        if (!requestingUser.IsAdmin)
        {
            throw new ForbiddenOperationException("Only an administrator can recover a continuation period.");
        }

        var key = new WeekKey(command.Year, command.WeekNumber);
        var closedPeriod = await weekRepository.GetByKeyAsync(key, cancellationToken)
            ?? throw new DomainValidationException("The requested week has not been generated yet.");
        if (closedPeriod.Status != WeekStatus.Closed)
        {
            throw new DomainValidationException("Only a closed period can be recovered.");
        }

        var closedAt = closedPeriod.ClosedAt
            ?? throw new DomainValidationException("The closed period has no closure timestamp to recover from.");
        var originalEndsAt = closedPeriod.SegmentNumber == 1
            ? key.StartDate.AddDays(6).ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc)
            : closedPeriod.EndsAt;
        if (closedAt >= originalEndsAt)
        {
            throw new DomainValidationException("This period was closed at its end and has no remaining time to recover.");
        }

        var continuation = new Week(
            key,
            await weekRepository.GetNextSegmentNumberAsync(key, cancellationToken),
            DateOnly.FromDateTime(closedAt),
            closedPeriod.EndDate,
            startsAt: closedAt,
            endsAt: originalEndsAt);

        var followingWeek = await weekRepository.GetByKeyAsync(key.Next(), cancellationToken);
        var carriedOverInstances = new List<TaskInstance>();
        if (followingWeek is not null)
        {
            var closedInstanceIds = (await taskInstanceRepository.GetByWeekAsync(closedPeriod.Id, cancellationToken))
                .Select(instance => instance.Id)
                .ToHashSet();
            var oldCarryOvers = (await taskInstanceRepository.GetByWeekAsync(followingWeek.Id, cancellationToken))
                .Where(instance => instance.IsCarryOver && instance.ParentTaskInstanceId is Guid parentId && closedInstanceIds.Contains(parentId))
                .ToList();
            if (oldCarryOvers.Any(instance => instance.IsCompleted))
            {
                throw new DomainValidationException("A carried-over task has already been completed in the following week and cannot be moved.");
            }

            foreach (var group in oldCarryOvers.GroupBy(instance => instance.TaskTemplateId))
            {
                foreach (var instance in group)
                {
                    instance.Cancel();
                }

                var source = group.OrderBy(instance => instance.PlannedDate).ThenBy(instance => instance.Id).First();
                carriedOverInstances.Add(new TaskInstance(
                    source.TaskTemplateId,
                    null,
                    continuation,
                    source.AssignedUserId,
                    continuation.StartDate,
                    continuation.StartDate,
                    isCarryOver: true,
                    parentTaskInstanceId: source.ParentTaskInstanceId));
            }
        }

        await weekRepository.AddAsync(continuation, cancellationToken);
        if (carriedOverInstances.Count > 0)
        {
            await taskInstanceRepository.AddRangeAsync(carriedOverInstances, cancellationToken);
        }
        await auditLogger.LogAsync(
            AuditEntityTypes.Week,
            continuation.Id,
            AuditActions.Recovered,
            requestingUser.Id,
            newValue: $"RecoveredFromPeriodId={closedPeriod.Id}; MigratedCarryOvers={carriedOverInstances.Count}",
            cancellationToken: cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return WeekMapper.ToDto(continuation);
    }
}
