using HomeTaskManager.Domain.Enums;
using HomeTaskManager.Domain.Entities;

namespace HomeTaskManager.Domain.Engine;

public sealed class BalancedPointsStrategy : IAssignmentStrategy
{
    public string Name => "BALANCED_POINTS";

    public AssignmentResult Assign(AssignmentContext context) =>
        BalancedAssignment.Assign(context, instance => TaskScoreCalculator.Calculate(context.TaskTemplatesById[instance.TaskTemplateId], context.Settings), Name);
}

public sealed class BalancedTimeStrategy : IAssignmentStrategy
{
    public string Name => "BALANCED_TIME";

    public AssignmentResult Assign(AssignmentContext context) =>
        BalancedAssignment.Assign(context, instance => context.TaskTemplatesById[instance.TaskTemplateId].EstimatedDurationMinutes ?? context.Settings.ReferenceDurationMinutes, Name);
}

internal static class BalancedAssignment
{
    public static AssignmentResult Assign(AssignmentContext context, Func<TaskInstance, decimal> weight, string strategyName)
    {
        var participants = context.ActiveParticipants.Where(participant => participant.AvailabilityPercentage > 0).ToList();
        if (participants.Count == 0) return new AssignmentResult([]);

        var loads = participants.ToDictionary(participant => participant.Id, _ => 0m);
        var counts = participants.ToDictionary(participant => participant.Id, _ => 0);
        var assignments = new List<TaskAssignment>();
        var dailyMandatoryOwners = new Dictionary<Guid, Guid>();
        // Seeded by the week itself (not by prior runs) so identical weeks do not always start
        // the rotation on the same participant.
        var tieBreakCursor = (context.Week.Year * 53 + context.Week.WeekNumber) % participants.Count;
        var grouped = context.TaskInstances
            .Where(instance => context.TaskTemplatesById.ContainsKey(instance.TaskTemplateId))
            .GroupBy(instance => instance.TaskTemplateId)
            .SelectMany(group => group.OrderBy(instance => instance.PlannedDate).Select((instance, index) => (instance, index, weight: weight(instance))))
            .OrderByDescending(item => item.weight)
            .ThenBy(item => item.instance.PlannedDate)
            .ThenBy(item => item.instance.Id);

        foreach (var item in grouped)
        {
            var template = context.TaskTemplatesById[item.instance.TaskTemplateId];
            var isSingleOwnerDaily = template.FrequencyType == FrequencyType.DailyMandatory && !template.DistributeDailyMandatory;

            Guid participantId;
            if (isSingleOwnerDaily && dailyMandatoryOwners.TryGetValue(template.Id, out var ownerId))
            {
                participantId = ownerId;
            }
            else
            {
                var minLoad = participants.Min(candidate => loads[candidate.Id] / (candidate.AvailabilityPercentage / 100m));
                var minCount = participants
                    .Where(candidate => loads[candidate.Id] / (candidate.AvailabilityPercentage / 100m) == minLoad)
                    .Min(candidate => counts[candidate.Id]);
                var tied = participants
                    .Where(candidate => loads[candidate.Id] / (candidate.AvailabilityPercentage / 100m) == minLoad && counts[candidate.Id] == minCount)
                    .OrderBy(candidate => candidate.Id)
                    .ToList();

                // Strict ties rotate the winner instead of always favoring the same participant id.
                participantId = tied[tieBreakCursor % tied.Count].Id;
                if (tied.Count > 1)
                {
                    tieBreakCursor++;
                }

                if (isSingleOwnerDaily)
                {
                    dailyMandatoryOwners[template.Id] = participantId;
                }
            }

            assignments.Add(new TaskAssignment(item.instance.Id, participantId));
            loads[participantId] += item.weight;
            counts[participantId]++;
        }

        return new AssignmentResult(assignments);
    }
}
