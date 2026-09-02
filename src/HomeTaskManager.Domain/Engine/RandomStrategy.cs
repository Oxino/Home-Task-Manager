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
        var participantNames = shuffledParticipants.ToDictionary(p => p.Id, p => p.DisplayName);
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

            var assignedId = shuffledParticipants[index % shuffledParticipants.Count].Id;
            var rotationPosition = (index % shuffledParticipants.Count) + 1;
            index++;
            if (isSingleOwnerDaily)
            {
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
