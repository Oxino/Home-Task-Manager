namespace HomeTaskManager.Domain.Engine;

using HomeTaskManager.Domain.Enums;

/// <summary>Assigns shuffled tasks into balanced participant slots without history or load scoring.</summary>
public sealed class RandomStrategy(Random? random = null) : IAssignmentStrategy
{
    private readonly Random _random = random ?? Random.Shared;

    public string Name => "RANDOM";

    public AssignmentResult Assign(AssignmentContext context)
    {
        var participants = context.ActiveParticipants.Where(participant => participant.AvailabilityPercentage > 0).ToList();
        if (participants.Count == 0)
        {
            return new AssignmentResult([]);
        }

        var shuffledInstances = context.TaskInstances.OrderBy(_ => _random.Next()).ToList();
        var shuffledParticipants = participants.OrderBy(_ => _random.Next()).ToList();
        var assignments = new List<TaskAssignment>();
        var dailyMandatoryOwners = new Dictionary<Guid, Guid>();
        var index = 0;

        foreach (var instance in shuffledInstances)
        {
            var isSingleOwnerDaily = context.TaskTemplatesById.TryGetValue(instance.TaskTemplateId, out var template) &&
                template.FrequencyType == FrequencyType.DailyMandatory &&
                !template.DistributeDailyMandatory;

            if (isSingleOwnerDaily && dailyMandatoryOwners.TryGetValue(instance.TaskTemplateId, out var ownerId))
            {
                assignments.Add(new TaskAssignment(instance.Id, ownerId));
                continue;
            }

            var assignedId = shuffledParticipants[index % shuffledParticipants.Count].Id;
            index++;
            if (isSingleOwnerDaily)
            {
                dailyMandatoryOwners[instance.TaskTemplateId] = assignedId;
            }

            assignments.Add(new TaskAssignment(instance.Id, assignedId));
        }

        return new AssignmentResult(assignments);
    }
}
