namespace HomeTaskManager.Domain.Engine;

using HomeTaskManager.Domain.Enums;

/// <summary>Assigns shuffled tasks into balanced participant slots without history or load scoring.</summary>
public sealed class RandomStrategy(Random? random = null) : IAssignmentStrategy
{
    private readonly Random _random = random ?? Random.Shared;

    public string Name => "RANDOM";

    public AssignmentResult Assign(AssignmentContext context)
    {
        // Step 1: only participants with availability > 0% can receive a task this run (someone on
        // full leave is excluded entirely rather than just deprioritized).
        var participants = context.ActiveParticipants.Where(participant => participant.AvailabilityPercentage > 0).ToList();
        if (participants.Count == 0)
        {
            return new AssignmentResult([]);
        }

        // Step 2: shuffle both the task order and the participant order independently. Shuffling the
        // instances means the round-robin below doesn't always hand out tasks in the same template/date
        // order; shuffling the participants means the rotation doesn't always start on the same person.
        var shuffledInstances = context.TaskInstances.OrderBy(_ => _random.Next()).ToList();
        var shuffledParticipants = participants.OrderBy(_ => _random.Next()).ToList();
        var participantNames = shuffledParticipants.ToDictionary(p => p.Id, p => p.DisplayName);
        var assignments = new List<TaskAssignment>();
        // Remembers, per template, which participant already "won" the random draw for a non-distributed
        // daily-mandatory task, so all 7 daily instances of that template stay with the same owner.
        var dailyMandatoryOwners = new Dictionary<Guid, Guid>();
        var index = 0;

        // Step 3: walk the shuffled instances one by one and assign each.
        foreach (var instance in shuffledInstances)
        {
            var isSingleOwnerDaily = context.TaskTemplatesById.TryGetValue(instance.TaskTemplateId, out var template) &&
                template.FrequencyType == FrequencyType.DailyMandatory &&
                !template.DistributeDailyMandatory;

            // Step 3a: a later instance of an already-decided single-owner daily template reuses the
            // same owner instead of taking part in the round-robin (it doesn't consume an `index` slot).
            if (isSingleOwnerDaily && dailyMandatoryOwners.TryGetValue(instance.TaskTemplateId, out var ownerId))
            {
                var groupSteps = new[]
                {
                    new ExplanationStep(
                        "SingleOwner",
                        "Responsabilité unique",
                        $"Tâche quotidienne obligatoire non répartie : le même propriétaire ({participantNames[ownerId]}) est conservé pour les 7 jours de la semaine, fixé par le premier tirage."),
                };
                assignments.Add(new TaskAssignment(instance.Id, ownerId, groupSteps));
                continue;
            }

            // Step 3b: plain round-robin over the shuffled participant list — no score, no duration,
            // no history is consulted at all, which is exactly what makes this strategy a neutral baseline.
            var assignedId = shuffledParticipants[index % shuffledParticipants.Count].Id;
            var rotationPosition = (index % shuffledParticipants.Count) + 1;
            index++; // Cycle through the shuffled participant order, wrapping back to the start once everyone has had a turn.
            if (isSingleOwnerDaily)
            {
                // First instance of this template decides the owner for the whole week (see step 3a above).
                dailyMandatoryOwners[instance.TaskTemplateId] = assignedId;
            }

            var steps = new[]
            {
                new ExplanationStep(
                    "Random",
                    "Tirage aléatoire",
                    $"Le moteur RANDOM ne tient compte ni des points, ni du temps, ni de l'historique : {participantNames[assignedId]} a été tiré(e) en position {rotationPosition}/{shuffledParticipants.Count} dans l'ordre mélangé des participants pour cette exécution."),
            };
            assignments.Add(new TaskAssignment(instance.Id, assignedId, steps));
        }

        return new AssignmentResult(assignments);
    }
}
