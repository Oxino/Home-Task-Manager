namespace HomeTaskManager.Domain.Engine;

using HomeTaskManager.Domain.Enums;

/// <summary>
/// V1 engine (ASSIGNMENT_ENGINE.md): balances score-weighted charge against availability,
/// penalizes recent repetitions, and breaks ties deterministically (BR-048).
/// </summary>
public sealed class IntelligentV1Strategy : IAssignmentStrategy
{
    public string Name => "INTELLIGENT_V1";

    public AssignmentResult Assign(AssignmentContext context)
    {
        var eligibleParticipants = context.ActiveParticipants.Where(p => p.AvailabilityPercentage > 0).ToList();
        if (eligibleParticipants.Count == 0)
        {
            return new AssignmentResult([]);
        }

        var settings = context.Settings;
        var runningScore = eligibleParticipants.ToDictionary(
            p => p.Id,
            p => context.HistoryByUserId.TryGetValue(p.Id, out var history) ? history.TotalScore : 0m);
        var runningAssignedCount = eligibleParticipants.ToDictionary(
            p => p.Id,
            p => context.HistoryByUserId.TryGetValue(p.Id, out var history) ? history.TotalAssignedCount : 0);

        var assignments = new List<TaskAssignment>();
        var dailyMandatoryAssignments = new Dictionary<Guid, Guid>();

        foreach (var instance in context.TaskInstances.OrderBy(i => i.PlannedDate).ThenBy(i => i.Id))
        {
            if (!context.TaskTemplatesById.TryGetValue(instance.TaskTemplateId, out var template))
            {
                continue;
            }

            var taskScore = TaskScoreCalculator.Calculate(template, settings);

            // A mandatory daily task is a weekly responsibility: all seven instances stay with one participant.
            if (template.FrequencyType == FrequencyType.DailyMandatory &&
                dailyMandatoryAssignments.TryGetValue(template.Id, out var dailyOwnerId))
            {
                assignments.Add(new TaskAssignment(instance.Id, dailyOwnerId));
                runningScore[dailyOwnerId] += taskScore;
                runningAssignedCount[dailyOwnerId] += 1;
                continue;
            }

            var candidate = eligibleParticipants
                .Select(participant => new
                {
                    Participant = participant,
                    RecentOccurrences = GetRecentOccurrences(context, participant.Id, template.Id),
                })
                .Select(x => new
                {
                    x.Participant,
                    x.RecentOccurrences,
                    AdjustedLoad = runningScore[x.Participant.Id] + x.RecentOccurrences * settings.RepetitionPenaltyWeight * taskScore,
                    Target = x.Participant.AvailabilityPercentage / 100m,
                })
                .OrderBy(x => x.AdjustedLoad / x.Target) // BR-040: charge ajustée rapportée à la cible
                .ThenBy(x => runningAssignedCount[x.Participant.Id]) // BR-048 tie-break 1
                .ThenBy(x => x.RecentOccurrences) // BR-048 tie-break 2
                .ThenBy(x => x.Participant.Id) // BR-048 tie-break 3
                .First()
                .Participant;

            assignments.Add(new TaskAssignment(instance.Id, candidate.Id));
            if (template.FrequencyType == FrequencyType.DailyMandatory)
            {
                dailyMandatoryAssignments[template.Id] = candidate.Id;
            }
            runningScore[candidate.Id] += taskScore;
            runningAssignedCount[candidate.Id] += 1;
        }

        return new AssignmentResult(assignments);
    }

    private static int GetRecentOccurrences(AssignmentContext context, Guid userId, Guid taskTemplateId) =>
        context.HistoryByUserId.TryGetValue(userId, out var history)
            && history.OccurrencesByTaskTemplate.TryGetValue(taskTemplateId, out var count)
            ? count
            : 0;
}
