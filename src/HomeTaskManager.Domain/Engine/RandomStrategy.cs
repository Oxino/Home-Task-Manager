namespace HomeTaskManager.Domain.Engine;

/// <summary>Baseline V1 strategy: assigns each task to a uniformly random active participant.</summary>
public sealed class RandomStrategy(Random? random = null) : IAssignmentStrategy
{
    private readonly Random _random = random ?? Random.Shared;

    public string Name => "RANDOM";

    public AssignmentResult Assign(AssignmentContext context)
    {
        if (context.ActiveParticipants.Count == 0)
        {
            return new AssignmentResult([]);
        }

        var assignments = context.TaskInstances
            .Select(instance => new TaskAssignment(
                instance.Id,
                context.ActiveParticipants[_random.Next(context.ActiveParticipants.Count)].Id))
            .ToList();

        return new AssignmentResult(assignments);
    }
}
