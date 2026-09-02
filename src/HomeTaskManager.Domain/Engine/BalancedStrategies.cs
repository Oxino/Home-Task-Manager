using HomeTaskManager.Domain.Enums;
using HomeTaskManager.Domain.Entities;

namespace HomeTaskManager.Domain.Engine;

public sealed class BalancedPointsStrategy : IAssignmentStrategy
{
    public string Name => "BALANCED_POINTS";

    public AssignmentResult Assign(AssignmentContext context) =>
        BalancedAssignment.Assign(context, instance => TaskScoreCalculator.Calculate(context.TaskTemplatesById[instance.TaskTemplateId], context.Settings), Name, "Score de la tâche", "pts");
}

public sealed class BalancedTimeStrategy : IAssignmentStrategy
{
    public string Name => "BALANCED_TIME";

    public AssignmentResult Assign(AssignmentContext context) =>
        BalancedAssignment.Assign(context, instance => context.TaskTemplatesById[instance.TaskTemplateId].EstimatedDurationMinutes ?? context.Settings.ReferenceDurationMinutes, Name, "Durée estimée", "min");
}

internal static class BalancedAssignment
{
    public static AssignmentResult Assign(AssignmentContext context, Func<TaskInstance, decimal> weight, string strategyName, string weightLabel, string weightUnit)
    {
        var participants = context.ActiveParticipants.Where(participant => participant.AvailabilityPercentage > 0).ToList();
        if (participants.Count == 0) return new AssignmentResult([]);

        var participantNames = participants.ToDictionary(p => p.Id, p => p.DisplayName);
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
            var scoreStep = new ExplanationStep("Score", weightLabel, $"{weightLabel} : {item.weight:0.##} {weightUnit}.");

            Guid participantId;
            List<ExplanationStep> steps;
            if (isSingleOwnerDaily && dailyMandatoryOwners.TryGetValue(template.Id, out var ownerId))
            {
                participantId = ownerId;
                steps =
                [
                    new ExplanationStep(
                        "SingleOwner",
                        "Responsabilité unique",
                        $"Tâche quotidienne obligatoire non répartie : le même propriétaire ({participantNames[ownerId]}) est conservé pour les 7 jours de la semaine."),
                ];
            }
            else
            {
                var normalizedLoadBefore = participants.ToDictionary(candidate => candidate.Id, candidate => loads[candidate.Id] / (candidate.AvailabilityPercentage / 100m));
                var minLoad = normalizedLoadBefore.Values.Min();
                var minCount = participants
                    .Where(candidate => normalizedLoadBefore[candidate.Id] == minLoad)
                    .Min(candidate => counts[candidate.Id]);
                var tied = participants
                    .Where(candidate => normalizedLoadBefore[candidate.Id] == minLoad && counts[candidate.Id] == minCount)
                    .OrderBy(candidate => candidate.Id)
                    .ToList();

                // Strict ties rotate the winner instead of always favoring the same participant id.
                participantId = tied[tieBreakCursor % tied.Count].Id;
                var tieBroken = tied.Count > 1;
                if (tieBroken)
                {
                    tieBreakCursor++;
                }

                if (isSingleOwnerDaily)
                {
                    dailyMandatoryOwners[template.Id] = participantId;
                }

                var candidates = participants
                    .OrderBy(candidate => normalizedLoadBefore[candidate.Id])
                    .Select(candidate => new ExplanationCandidate(
                        candidate.Id,
                        participantNames[candidate.Id],
                        normalizedLoadBefore[candidate.Id],
                        candidate.Id == participantId,
                        candidate.Id == participantId
                            ? null
                            : normalizedLoadBefore[candidate.Id] > minLoad
                                ? $"charge normalisée plus élevée ({normalizedLoadBefore[candidate.Id]:0.##})"
                                : counts[candidate.Id] > minCount
                                    ? "plus de tâches déjà affectées"
                                    : "non retenu(e) sur cette rotation"))
                    .ToList();

                var loadStep = new ExplanationStep(
                    "Load",
                    "Comparaison des charges",
                    $"{participantNames[participantId]} a la charge normalisée la plus faible ({minLoad:0.##}).",
                    candidates);

                steps = [scoreStep, loadStep];

                if (tieBroken)
                {
                    var tiedNames = string.Join(", ", tied.Select(p => participantNames[p.Id]));
                    steps.Add(new ExplanationStep(
                        "TieBreak",
                        "Égalité départagée",
                        $"Égalité de charge et de nombre de tâches entre {tiedNames} ; la rotation hebdomadaire (BR-062) attribue ce tour à {participantNames[participantId]}."));
                }
            }

            assignments.Add(new TaskAssignment(item.instance.Id, participantId, steps));
            loads[participantId] += item.weight;
            counts[participantId]++;
        }

        return new AssignmentResult(assignments);
    }
}

