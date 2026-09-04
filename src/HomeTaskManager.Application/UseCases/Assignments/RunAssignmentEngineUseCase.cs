using HomeTaskManager.Application.Abstractions;
using HomeTaskManager.Application.Abstractions.Repositories;
using HomeTaskManager.Application.Exceptions;
using HomeTaskManager.Application.UseCases.Settings;
using HomeTaskManager.Application.UseCases.Weeks;
using HomeTaskManager.Domain.Engine;
using HomeTaskManager.Domain.Entities;
using HomeTaskManager.Domain.Enums;
using HomeTaskManager.Domain.Exceptions;
using HomeTaskManager.Domain.ValueObjects;
using System.Text.Json;

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
    IAssignmentEngineSettingsProvider settingsProvider,
    IAuditLogger auditLogger,
    IUnitOfWork unitOfWork,
    INotificationRepository? notificationRepository = null,
    IUserPreferencesRepository? userPreferencesRepository = null,
    INotificationService? notificationService = null,
    IAssignmentExplanationRepository? assignmentExplanationRepository = null,
    IEnumerable<IAssignmentStrategy>? assignmentStrategies = null) : IRunAssignmentEngineUseCase
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

        // ADR-005: carried-over instances keep their original owner; only orphaned carry-overs need assignment.
        var assignableInstances = allInstances
            .Where(instance => instance.Status != TaskInstanceStatus.Cancelled)
            .Where(instance => !instance.IsCarryOver || instance.AssignedUserId is null)
            .ToList();

        var settings = await settingsProvider.GetAsync(cancellationToken);
        // Resolve the admin-configured strategy by name among all registered IAssignmentStrategy implementations
        // (falls back to the injected default when the DI container only wires a single strategy, e.g. in tests).
        var selectedStrategy = assignmentStrategies?.FirstOrDefault(strategy => strategy.Name == settings.StrategyName) ?? assignmentStrategy;
        if (assignmentStrategies is not null && !assignmentStrategies.Any(strategy => strategy.Name == settings.StrategyName))
        {
            throw new DomainValidationException($"The assignment engine '{settings.StrategyName}' is not registered.");
        }

        var run = new AssignmentRun(
            week.Id,
            selectedStrategy.Name,
            requestingUser.Id,
            isAutomatic: false,
            notes: activeParticipants.Count == 0 ? "No active participant available." : null);

        if (activeParticipants.Count > 0 && assignableInstances.Count > 0)
        {
            // BR-053: equity window, defaults to 8 weeks before the target week; the run being (re)assigned is excluded.
            var since = week.StartDate.AddDays(-7 * settings.EquityWindowWeeks);
            var historicalInstances = week.IgnoreHistoryForAssignment
                ? []
                : (await taskInstanceRepository.GetAssignedSinceAsync(since, cancellationToken))
                    .Where(instance => instance.WeekId != week.Id && !instance.IsCarryOver)
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
            var result = selectedStrategy.Assign(context);
            var instancesById = assignableInstances.ToDictionary(instance => instance.Id);
            var notifiedAssignments = new HashSet<(Guid UserId, Guid TaskTemplateId)>();
            var participantNames = activeParticipants.ToDictionary(p => p.Id, p => p.DisplayName);

            if (assignmentExplanationRepository is not null)
            {
                // ADR-013: a relaunch overwrites the week's assignment, so any previous decision trace for these instances is stale.
                await assignmentExplanationRepository.RemoveByTaskInstanceIdsAsync(
                    result.Assignments.Select(a => a.TaskInstanceId), cancellationToken);
            }

            foreach (var assignment in result.Assignments)
            {
                var assignedInstance = instancesById[assignment.TaskInstanceId];
                assignedInstance.Assign(run.Id, assignment.AssignedUserId);

                if (assignmentExplanationRepository is not null && assignment.ExplanationSteps is { Count: > 0 })
                {
                    var assignedName = participantNames.TryGetValue(assignment.AssignedUserId, out var name) ? name : "ce participant";
                    var summaryText = $"Affectée à {assignedName} par le moteur {selectedStrategy.Name}.";
                    var stepsJson = JsonSerializer.Serialize(assignment.ExplanationSteps);
                    await assignmentExplanationRepository.AddAsync(
                        new AssignmentExplanation(assignedInstance.Id, run.Id, assignment.AssignedUserId, selectedStrategy.Name, summaryText, stepsJson),
                        cancellationToken);
                }

                var notificationPreference = userPreferencesRepository is null
                    ? "All"
                    : (await userPreferencesRepository.GetByUserIdAsync(assignment.AssignedUserId, cancellationToken))?.NotificationPreference ?? "All";
                if (notificationRepository is not null &&
                    notificationPreference == "All" &&
                    notifiedAssignments.Add((assignment.AssignedUserId, assignedInstance.TaskTemplateId)) &&
                    templatesById.TryGetValue(assignedInstance.TaskTemplateId, out var assignedTemplate))
                {
                    var title = "Nouvelle tâche affectée";
                    var message = $"{assignedTemplate.Title} vous a été affectée pour la semaine {week.WeekNumber}.";
                    await notificationRepository.AddAsync(new Notification(
                        assignment.AssignedUserId,
                        title,
                        message,
                        $"/week?year={week.Year}&week={week.WeekNumber}&instanceId={assignedInstance.Id}",
                        Domain.Enums.NotificationImportance.Normal), cancellationToken);
                    if (notificationService is not null)
                    {
                        await notificationService.SendNotificationAsync(
                            assignment.AssignedUserId,
                            title,
                            message,
                            Domain.Enums.NotificationImportance.Normal,
                            cancellationToken);
                    }
                }
            }
        }

        var assignedCount = assignableInstances.Count(instance => instance.AssignmentRunId == run.Id);
        await auditLogger.LogAsync(
            AuditEntityTypes.AssignmentRun, run.Id, AuditActions.Executed, requestingUser.Id,
            newValue: $"WeekId={week.Id}; Strategy={run.StrategyName}; AssignedCount={assignedCount}", cancellationToken: cancellationToken);

        await assignmentRunRepository.AddAsync(run, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new RunAssignmentEngineResult(AssignmentRunMapper.ToDto(run), allInstances.Select(WeekMapper.ToDto).ToList());
    }
}
