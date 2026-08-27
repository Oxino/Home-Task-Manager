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
        var grouped = context.TaskInstances
            .Where(instance => context.TaskTemplatesById.ContainsKey(instance.TaskTemplateId))
            .GroupBy(instance => instance.TaskTemplateId)
            .SelectMany(group => group.OrderBy(instance => instance.PlannedDate).Select((instance, index) => (instance, index, weight: weight(instance))))
            .OrderByDescending(item => item.weight)
            .ThenBy(item => item.instance.PlannedDate)
            .ThenBy(item => item.instance.Id);

        foreach (var item in grouped)
        {
            var participant = participants
                .OrderBy(candidate => loads[candidate.Id] / (candidate.AvailabilityPercentage / 100m))
                .ThenBy(candidate => counts[candidate.Id])
                .ThenBy(candidate => candidate.Id)
                .First();
            assignments.Add(new TaskAssignment(item.instance.Id, participant.Id));
            loads[participant.Id] += item.weight;
            counts[participant.Id]++;
        }

        return new AssignmentResult(assignments);
    }
}
