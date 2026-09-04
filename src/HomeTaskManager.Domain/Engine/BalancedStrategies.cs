using HomeTaskManager.Domain.Enums;
using HomeTaskManager.Domain.Entities;

namespace HomeTaskManager.Domain.Engine;

/// <summary>Greedy-balancing strategy that weighs each task by its business score (priority/duration multipliers) and always gives the next task to the currently least-loaded participant.</summary>
public sealed class BalancedPointsStrategy : IAssignmentStrategy
{
    public string Name => "BALANCED_POINTS";

    // The "weight" used to order/accumulate load is the BR-052 business score (difficulty x priority x duration factor),
    // so a hard, high-priority, long task outweighs an easy quick one when deciding who is "most loaded".
    public AssignmentResult Assign(AssignmentContext context) =>
        BalancedAssignment.Assign(context, instance => TaskScoreCalculator.Calculate(context.TaskTemplatesById[instance.TaskTemplateId], context.Settings), Name, "Score de la tâche", "pts");
}

/// <summary>Same greedy-balancing algorithm as <see cref="BalancedPointsStrategy"/>, but weighs each task by its estimated duration (minutes) instead of its business score.</summary>
public sealed class BalancedTimeStrategy : IAssignmentStrategy
{
    public string Name => "BALANCED_TIME";

    // The "weight" here is raw estimated minutes (falling back to the settings' reference duration when a
    // template has none), so this strategy balances time spent rather than perceived difficulty/priority.
    public AssignmentResult Assign(AssignmentContext context) =>
        BalancedAssignment.Assign(context, instance => context.TaskTemplatesById[instance.TaskTemplateId].EstimatedDurationMinutes ?? context.Settings.ReferenceDurationMinutes, Name, "Durée estimée", "min");
}

/// <summary>
/// Shared greedy load-balancing algorithm reused by <see cref="BalancedPointsStrategy"/> and <see cref="BalancedTimeStrategy"/>.
/// Tasks are processed heaviest-first (by <paramref name="weight"/>... see below) and each one goes to whichever eligible
/// participant currently has the lowest normalized load (load divided by their availability percentage), so part-time
/// participants are not overloaded relative to their availability.
/// </summary>
internal static class BalancedAssignment
{
    /// <param name="context">Week, participants, instances and settings to assign from.</param>
    /// <param name="weight">Per-instance weight used to both order tasks and accumulate load (score or duration depending on the caller).</param>
    /// <param name="strategyName">Name recorded on the run (unused internally beyond documentation of intent).</param>
    /// <param name="weightLabel">Human-readable label shown in the assignment explanation (BR-033).</param>
    /// <param name="weightUnit">Unit suffix shown alongside the weight in the explanation.</param>
    public static AssignmentResult Assign(AssignmentContext context, Func<TaskInstance, decimal> weight, string strategyName, string weightLabel, string weightUnit)
    {
        // Step 1: only participants with availability > 0% are eligible this run (e.g. on leave = excluded, not just deprioritized).
        var participants = context.ActiveParticipants.Where(participant => participant.AvailabilityPercentage > 0).ToList();
        if (participants.Count == 0) return new AssignmentResult([]);

        var participantNames = participants.ToDictionary(p => p.Id, p => p.DisplayName);
        // Step 2: running totals updated as tasks are handed out; `loads` drives the "least loaded" comparison,
        // `counts` is only used as a secondary tie-breaker.
        var loads = participants.ToDictionary(participant => participant.Id, _ => 0m);
        var counts = participants.ToDictionary(participant => participant.Id, _ => 0);
        var assignments = new List<TaskAssignment>();
        // Remembers, per template, which participant already "owns" a non-distributed daily-mandatory
        // task so all 7 daily instances stay with the same person instead of being load-balanced.
        var dailyMandatoryOwners = new Dictionary<Guid, Guid>();
        // Step 3: pick a deterministic-but-varying starting point for tie-break rotation, derived from the
        // week itself (year/week number) rather than from any prior run, so replaying the exact same week
        // always resolves ties the same way, but different weeks don't all favor the same participant.
        var tieBreakCursor = (context.Week.Year * 53 + context.Week.WeekNumber) % participants.Count;
        // Step 4: process the heaviest tasks first (bin-packing heuristic): assigning big tasks early
        // when load is still near zero gives the balancer more room to correct small tasks later. Within
        // a template, instances are still ordered by planned date so e.g. "Monday" is picked before "Tuesday".
        var grouped = context.TaskInstances
            .Where(instance => context.TaskTemplatesById.ContainsKey(instance.TaskTemplateId))
            .GroupBy(instance => instance.TaskTemplateId)
            .SelectMany(group => group.OrderBy(instance => instance.PlannedDate).Select((instance, index) => (instance, index, weight: weight(instance))))
            .OrderByDescending(item => item.weight)
            .ThenBy(item => item.instance.PlannedDate)
            .ThenBy(item => item.instance.Id);

        // Step 5: assign each task instance, heaviest first, to the currently least-loaded eligible participant.
        foreach (var item in grouped)
        {
            var template = context.TaskTemplatesById[item.instance.TaskTemplateId];
            var isSingleOwnerDaily = template.FrequencyType == FrequencyType.DailyMandatory && !template.DistributeDailyMandatory;
            var scoreStep = new ExplanationStep("Score", weightLabel, $"{weightLabel} : {item.weight:0.##} {weightUnit}.");

            Guid participantId;
            List<ExplanationStep> steps;
            if (isSingleOwnerDaily && dailyMandatoryOwners.TryGetValue(template.Id, out var ownerId))
            {
                // Step 5a: this template's owner was already decided by an earlier instance in the loop (e.g. Monday);
                // reuse it for every other day of the week instead of re-running the load comparison below.
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
                // Step 5b: normalize each participant's accumulated load by their availability so a
                // half-time participant is considered "equally loaded" at half the raw total (e.g. 50 pts
                // at 50% availability normalizes to 100, same as 100 pts for a full-time participant).
                var normalizedLoadBefore = participants.ToDictionary(candidate => candidate.Id, candidate => loads[candidate.Id] / (candidate.AvailabilityPercentage / 100m));
                var minLoad = normalizedLoadBefore.Values.Min();
                // Among the least-loaded participants, prefer whoever has fewer tasks assigned so far
                // (a secondary tie-breaker before the rotating cursor below).
                var minCount = participants
                    .Where(candidate => normalizedLoadBefore[candidate.Id] == minLoad)
                    .Min(candidate => counts[candidate.Id]);
                var tied = participants
                    .Where(candidate => normalizedLoadBefore[candidate.Id] == minLoad && counts[candidate.Id] == minCount)
                    .OrderBy(candidate => candidate.Id)
                    .ToList();

                // Step 5c: strict ties (same normalized load AND same task count) rotate the winner via the
                // cursor instead of always favoring the lowest participant id, then advance the cursor so the
                // next tie in this same run picks a different participant among the tied ones.
                participantId = tied[tieBreakCursor % tied.Count].Id;
                var tieBroken = tied.Count > 1;
                if (tieBroken)
                {
                    tieBreakCursor++;
                }

                if (isSingleOwnerDaily)
                {
                    // This is the first instance of a non-distributed daily-mandatory template encountered:
                    // lock in today's winner as the owner for the rest of the week (see step 5a).
                    dailyMandatoryOwners[template.Id] = participantId;
                }

                // Step 5d: build the BR-033 explanation candidates list (every participant, sorted by load,
                // with a human-readable reason for why each non-winner was passed over).
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

            // Step 6: commit the decision and update the running totals used by the next iteration.
            assignments.Add(new TaskAssignment(item.instance.Id, participantId, steps));
            loads[participantId] += item.weight;
            counts[participantId]++;
        }

        return new AssignmentResult(assignments);
    }
}

