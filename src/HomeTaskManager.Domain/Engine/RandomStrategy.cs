namespace HomeTaskManager.Domain.Engine;

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
        var assignments = shuffledInstances.Select((instance, index) => new TaskAssignment(
            instance.Id,
            shuffledParticipants[index % shuffledParticipants.Count].Id)).ToList();

        return new AssignmentResult(assignments);
    }
}
