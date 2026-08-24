using HomeTaskManager.Application.Abstractions;
using HomeTaskManager.Application.Abstractions.Repositories;
using HomeTaskManager.Application.Exceptions;
using HomeTaskManager.Application.UseCases.Weeks;
using HomeTaskManager.Domain.Engine;
using HomeTaskManager.Domain.Entities;
using HomeTaskManager.Domain.Enums;
using HomeTaskManager.Domain.Exceptions;
using HomeTaskManager.Domain.ValueObjects;

namespace HomeTaskManager.Application.UseCases.Assignments;

public sealed record RunAssignmentEngineCommand(int Year, int WeekNumber, Guid TriggeredByUserId);

public sealed record RunAssignmentEngineResult(AssignmentRunDto Run, IReadOnlyList<TaskInstanceDto> Instances);

public interface IRunAssignmentEngineUseCase
{
    Task<RunAssignmentEngineResult> ExecuteAsync(RunAssignmentEngineCommand command, CancellationToken cancellationToken = default);
}

/// <summary>UC-ENG-01/UC-ENG-03: assigns (or re-assigns, ADR-013) a week's task instances to active participants.</summary>
public sealed class RunAssignmentEngineUseCase(
    IWeekRepository weekRepository,
    IUserRepository userRepository,
    ITaskTemplateRepository taskTemplateRepository,
    ITaskInstanceRepository taskInstanceRepository,
    IAssignmentRunRepository assignmentRunRepository,
    IAssignmentStrategy assignmentStrategy,
    IUnitOfWork unitOfWork) : IRunAssignmentEngineUseCase
{
    public async Task<RunAssignmentEngineResult> ExecuteAsync(
        RunAssignmentEngineCommand command,
        CancellationToken cancellationToken = default)
    {
        var requestingUser = await userRepository.GetByIdAsync(command.TriggeredByUserId, cancellationToken)
            ?? throw new DomainValidationException("The triggering profile does not exist.");

        // UC-ENG-03: only an administrator can (re-)run the assignment engine.
        if (!requestingUser.IsAdmin)
        {
            throw new ForbiddenOperationException("Only an administrator can run the assignment engine.");
        }

        var key = new WeekKey(command.Year, command.WeekNumber);
        var week = await weekRepository.GetByKeyAsync(key, cancellationToken)
            ?? throw new DomainValidationException("The week has not been generated yet.");

        var activeParticipants = await userRepository.GetActiveAsync(cancellationToken);
        var allInstances = await taskInstanceRepository.GetByWeekAsync(week.Id, cancellationToken);

        // ADR-005: carried-over instances keep their original owner and are not touched by the engine.
        var assignableInstances = allInstances
            .Where(instance => !instance.IsCarryOver && instance.Status != TaskInstanceStatus.Cancelled)
            .ToList();

        var run = new AssignmentRun(
            week.Id,
            assignmentStrategy.Name,
            requestingUser.Id,
            isAutomatic: false,
            notes: activeParticipants.Count == 0 ? "No active participant available." : null);

        if (activeParticipants.Count > 0 && assignableInstances.Count > 0)
        {
            var settings = AssignmentEngineSettings.Default;

            // BR-053: equity window, defaults to 8 weeks before the target week; the run being (re)assigned is excluded.
            var since = week.StartDate.AddDays(-7 * settings.EquityWindowWeeks);
            var historicalInstances = (await taskInstanceRepository.GetAssignedSinceAsync(since, cancellationToken))
                .Where(instance => instance.WeekId != week.Id)
                .ToList();

            var templateIds = assignableInstances.Select(i => i.TaskTemplateId)
                .Concat(historicalInstances.Select(i => i.TaskTemplateId))
                .Distinct();
            var templatesById = (await taskTemplateRepository.GetByIdsAsync(templateIds, cancellationToken))
                .ToDictionary(t => t.Id);

            var historyByUserId = historicalInstances
                .GroupBy(instance => instance.AssignedUserId!.Value)
                .ToDictionary(
                    group => group.Key,
                    group => new ParticipantHistory(
                        group.Key,
                        group.Sum(i => templatesById.TryGetValue(i.TaskTemplateId, out var t) ? TaskScoreCalculator.Calculate(t, settings) : 0m),
                        group.Count(),
                        group.GroupBy(i => i.TaskTemplateId).ToDictionary(g => g.Key, g => g.Count())));

            var context = new AssignmentContext(week, activeParticipants, assignableInstances, templatesById, historyByUserId, settings);
            var result = assignmentStrategy.Assign(context);
            var instancesById = assignableInstances.ToDictionary(instance => instance.Id);

            foreach (var assignment in result.Assignments)
            {
                instancesById[assignment.TaskInstanceId].Assign(run.Id, assignment.AssignedUserId);
            }
        }

        await assignmentRunRepository.AddAsync(run, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new RunAssignmentEngineResult(AssignmentRunMapper.ToDto(run), allInstances.Select(WeekMapper.ToDto).ToList());
    }
}
