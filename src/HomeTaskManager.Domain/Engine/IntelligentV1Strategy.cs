namespace HomeTaskManager.Domain.Engine;

using HomeTaskManager.Domain.Entities;
using HomeTaskManager.Domain.Enums;

/// <summary>
/// V1 engine (ASSIGNMENT_ENGINE.md): builds an initial weekly allocation with a greedy
/// largest-first construction (LPT), then refines it with a bounded local search (single moves
/// and pairwise swaps) that minimizes the variance of normalized adjusted load across
/// participants (BR-048). Unlike a single chronological pass, this lets an early decision be
/// corrected later in the same run instead of permanently locking in an imbalance.
/// </summary>
public sealed class IntelligentV1Strategy : IAssignmentStrategy
{
    private const int MaxLocalSearchIterations = 500;
    private const decimal ImprovementEpsilon = 0.0000001m;

    public string Name => "INTELLIGENT_V1";

    public AssignmentResult Assign(AssignmentContext context)
    {
        var participants = context.ActiveParticipants
            .Where(p => p.AvailabilityPercentage > 0)
            .OrderBy(p => p.Id)
            .ToList();
        if (participants.Count == 0)
        {
            return new AssignmentResult([]);
        }

        var items = BuildItems(context, participants);
        if (items.Count == 0)
        {
            return new AssignmentResult([]);
        }

        var baseline = participants.ToDictionary(
            p => p.Id,
            p => context.HistoryByUserId.TryGetValue(p.Id, out var history) ? history.TotalScore : 0m);
        var baselineCount = participants.ToDictionary(
            p => p.Id,
            p => context.HistoryByUserId.TryGetValue(p.Id, out var history) ? history.TotalAssignedCount : 0);
        var target = participants.ToDictionary(p => p.Id, p => p.AvailabilityPercentage / 100m);

        var assignment = Construct(items, participants, baseline, baselineCount, target, context.Settings);
        Improve(items, participants, baseline, target, context.Settings, assignment);

        var assignments = items
            .SelectMany(item => item.InstanceIds.Select(instanceId => new TaskAssignment(instanceId, assignment[item.Id])))
            .ToList();

        return new AssignmentResult(assignments);
    }

    /// <summary>Groups a non-distributed mandatory daily task's 7 instances into a single item so it always moves as one block.</summary>
    private static List<AllocationItem> BuildItems(AssignmentContext context, IReadOnlyList<User> participants)
    {
        var settings = context.Settings;
        var items = new List<AllocationItem>();
        var dailyMandatoryGroups = new Dictionary<Guid, List<TaskInstance>>();

        foreach (var instance in context.TaskInstances)
        {
            if (!context.TaskTemplatesById.TryGetValue(instance.TaskTemplateId, out var template))
            {
                continue;
            }

            if (template.FrequencyType == FrequencyType.DailyMandatory && !template.DistributeDailyMandatory)
            {
                if (!dailyMandatoryGroups.TryGetValue(template.Id, out var group))
                {
                    group = [];
                    dailyMandatoryGroups[template.Id] = group;
                }
                group.Add(instance);
                continue;
            }

            var score = TaskScoreCalculator.Calculate(template, settings);
            var occurrences = BuildOccurrenceMap(context, participants, template.Id);
            items.Add(new AllocationItem(instance.Id, [instance.Id], score, instance.PlannedDate, occurrences));
        }

        foreach (var (templateId, instances) in dailyMandatoryGroups)
        {
            var template = context.TaskTemplatesById[templateId];
            var perInstanceScore = TaskScoreCalculator.Calculate(template, settings);
            var instanceIds = instances.Select(i => i.Id).OrderBy(id => id).ToList();
            var earliestDate = instances.Min(i => i.PlannedDate);
            var occurrences = BuildOccurrenceMap(context, participants, templateId);
            items.Add(new AllocationItem(instanceIds[0], instanceIds, perInstanceScore * instances.Count, earliestDate, occurrences));
        }

        return items.OrderBy(item => item.EarliestPlannedDate).ThenBy(item => item.Id).ToList();
    }

    private static Dictionary<Guid, int> BuildOccurrenceMap(AssignmentContext context, IReadOnlyList<User> participants, Guid taskTemplateId) =>
        participants.ToDictionary(
            p => p.Id,
            p => context.HistoryByUserId.TryGetValue(p.Id, out var history)
                && history.OccurrencesByTaskTemplate.TryGetValue(taskTemplateId, out var count)
                ? count
                : 0);

    /// <summary>Largest-first (LPT) construction: the heaviest items are placed while participants are still close to even.</summary>
    private static Dictionary<Guid, Guid> Construct(
        IReadOnlyList<AllocationItem> items,
        IReadOnlyList<User> participants,
        IReadOnlyDictionary<Guid, decimal> baseline,
        IReadOnlyDictionary<Guid, int> baselineCount,
        IReadOnlyDictionary<Guid, decimal> target,
        AssignmentEngineSettings settings)
    {
        var assignment = new Dictionary<Guid, Guid>();
        var load = new Dictionary<Guid, decimal>(baseline);
        var count = new Dictionary<Guid, int>(baselineCount);

        foreach (var item in items.OrderByDescending(i => i.Score).ThenBy(i => i.EarliestPlannedDate).ThenBy(i => i.Id))
        {
            var candidate = participants
                .OrderBy(p => (load[p.Id] + item.AdjustedContribution(p.Id, settings)) / target[p.Id])
                .ThenBy(p => count[p.Id])
                .ThenBy(p => item.RecentOccurrences(p.Id))
                .ThenBy(p => p.Id)
                .First();

            assignment[item.Id] = candidate.Id;
            load[candidate.Id] += item.Score;
            count[candidate.Id] += item.InstanceIds.Count;
        }

        return assignment;
    }

    /// <summary>Bounded hill-climbing: repeatedly applies the best single move or swap that reduces the load variance.</summary>
    private static void Improve(
        IReadOnlyList<AllocationItem> items,
        IReadOnlyList<User> participants,
        IReadOnlyDictionary<Guid, decimal> baseline,
        IReadOnlyDictionary<Guid, decimal> target,
        AssignmentEngineSettings settings,
        Dictionary<Guid, Guid> assignment)
    {
        if (items.Count < 2 || participants.Count < 2)
        {
            return;
        }

        var currentObjective = ComputeObjective(items, participants, baseline, target, settings, assignment);

        for (var iteration = 0; iteration < MaxLocalSearchIterations; iteration++)
        {
            Guid? bestItemA = null;
            Guid? bestItemB = null;
            var bestNewOwnerForA = Guid.Empty;
            var bestObjective = currentObjective;

            foreach (var item in items)
            {
                var currentOwner = assignment[item.Id];
                foreach (var participant in participants)
                {
                    if (participant.Id == currentOwner)
                    {
                        continue;
                    }

                    assignment[item.Id] = participant.Id;
                    var candidateObjective = ComputeObjective(items, participants, baseline, target, settings, assignment);
                    if (candidateObjective < bestObjective - ImprovementEpsilon)
                    {
                        bestObjective = candidateObjective;
                        bestItemA = item.Id;
                        bestItemB = null;
                        bestNewOwnerForA = participant.Id;
                    }

                    assignment[item.Id] = currentOwner;
                }
            }

            for (var i = 0; i < items.Count; i++)
            {
                for (var j = i + 1; j < items.Count; j++)
                {
                    var itemA = items[i];
                    var itemB = items[j];
                    var ownerA = assignment[itemA.Id];
                    var ownerB = assignment[itemB.Id];
                    if (ownerA == ownerB)
                    {
                        continue;
                    }

                    assignment[itemA.Id] = ownerB;
                    assignment[itemB.Id] = ownerA;
                    var candidateObjective = ComputeObjective(items, participants, baseline, target, settings, assignment);
                    if (candidateObjective < bestObjective - ImprovementEpsilon)
                    {
                        bestObjective = candidateObjective;
                        bestItemA = itemA.Id;
                        bestItemB = itemB.Id;
                    }

                    assignment[itemA.Id] = ownerA;
                    assignment[itemB.Id] = ownerB;
                }
            }

            if (bestItemA is null)
            {
                break;
            }

            if (bestItemB is null)
            {
                assignment[bestItemA.Value] = bestNewOwnerForA;
            }
            else
            {
                (assignment[bestItemA.Value], assignment[bestItemB.Value]) = (assignment[bestItemB.Value], assignment[bestItemA.Value]);
            }

            currentObjective = bestObjective;
        }
    }

    private static decimal ComputeObjective(
        IReadOnlyList<AllocationItem> items,
        IReadOnlyList<User> participants,
        IReadOnlyDictionary<Guid, decimal> baseline,
        IReadOnlyDictionary<Guid, decimal> target,
        AssignmentEngineSettings settings,
        IReadOnlyDictionary<Guid, Guid> assignment)
    {
        var normalizedLoad = participants.ToDictionary(p => p.Id, p => baseline[p.Id] / target[p.Id]);
        foreach (var item in items)
        {
            var owner = assignment[item.Id];
            normalizedLoad[owner] += item.AdjustedContribution(owner, settings) / target[owner];
        }

        var mean = normalizedLoad.Values.Average();
        return normalizedLoad.Values.Sum(value => (value - mean) * (value - mean));
    }

    /// <summary>A unit of work for the optimizer: one instance, or all 7 instances of a locked mandatory daily task.</summary>
    private sealed class AllocationItem(
        Guid id,
        IReadOnlyList<Guid> instanceIds,
        decimal score,
        DateOnly earliestPlannedDate,
        IReadOnlyDictionary<Guid, int> recentOccurrencesByParticipant)
    {
        public Guid Id { get; } = id;

        public IReadOnlyList<Guid> InstanceIds { get; } = instanceIds;

        public decimal Score { get; } = score;

        public DateOnly EarliestPlannedDate { get; } = earliestPlannedDate;

        public int RecentOccurrences(Guid participantId) =>
            recentOccurrencesByParticipant.TryGetValue(participantId, out var count) ? count : 0;

        /// <summary>Score plus the anti-repetition penalty (BR-054) for this specific participant.</summary>
        public decimal AdjustedContribution(Guid participantId, AssignmentEngineSettings settings) =>
            Score + RecentOccurrences(participantId) * settings.RepetitionPenaltyWeight * Score;
    }
}

