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
        // Step 1: only participants with availability > 0% take part in this run, sorted by Id for
        // deterministic tie-breaking further down (LINQ grouping/ordering otherwise has no guaranteed order).
        var participants = context.ActiveParticipants
            .Where(p => p.AvailabilityPercentage > 0)
            .OrderBy(p => p.Id)
            .ToList();
        if (participants.Count == 0)
        {
            return new AssignmentResult([]);
        }

        // Step 2: turn the raw task instances into "allocation items" — one item per instance, except
        // non-distributed daily-mandatory templates whose 7 instances are merged into a single item
        // (see BuildItems) so the optimizer always moves/swaps all 7 days together.
        var items = BuildItems(context, participants);
        if (items.Count == 0)
        {
            return new AssignmentResult([]);
        }

        // Step 3: seed each participant's starting load/count from their recent history (BR-053 equity
        // window) instead of zero, so someone who was already heavily loaded in prior weeks keeps being
        // treated as loaded even before this run's tasks are added.
        var baseline = participants.ToDictionary(
            p => p.Id,
            p => context.HistoryByUserId.TryGetValue(p.Id, out var history) ? history.TotalScore : 0m);
        var baselineCount = participants.ToDictionary(
            p => p.Id,
            p => context.HistoryByUserId.TryGetValue(p.Id, out var history) ? history.TotalAssignedCount : 0);
        // Step 4: each participant's "target" is their availability as a fraction (e.g. 50% -> 0.5), used
        // to normalize load so a part-time participant isn't expected to carry a full-time share.
        var target = participants.ToDictionary(p => p.Id, p => p.AvailabilityPercentage / 100m);

        var participantNames = participants.ToDictionary(p => p.Id, p => p.DisplayName);
        // Step 5: build an initial allocation with the greedy LPT construction (heaviest items first).
        var (assignment, decisions) = Construct(items, participants, baseline, baselineCount, target, context.Settings);
        // Keep a snapshot of the construction result so BuildExplanationSteps can later say "this was
        // moved from X to Y during optimization" whenever Improve changes an owner.
        var constructionAssignment = new Dictionary<Guid, Guid>(assignment);
        // Step 6: refine that initial allocation in place with bounded local search (single moves + swaps).
        Improve(items, participants, baseline, target, context.Settings, assignment);

        // Step 7: expand each allocation item back into its individual TaskInstance assignments (a grouped
        // daily-mandatory item fans out into its 7 instances, all pointing to the same final owner), and
        // attach the BR-033 explanation trace for each.
        var assignments = items
            .SelectMany(item => item.InstanceIds.Select(instanceId => new TaskAssignment(
                instanceId,
                assignment[item.Id],
                BuildExplanationSteps(item, assignment[item.Id], constructionAssignment[item.Id], decisions[item.Id], participantNames, context.Settings))))
            .ToList();

        return new AssignmentResult(assignments);
    }

    private static List<ExplanationStep> BuildExplanationSteps(
        AllocationItem item,
        Guid finalOwnerId,
        Guid constructionOwnerId,
        ConstructionDecision decision,
        IReadOnlyDictionary<Guid, string> participantNames,
        AssignmentEngineSettings settings)
    {
        // Step 1: explain the BR-052 score formula for this item (difficulty x priority x duration), with a
        // slightly different wording when it's a grouped daily-mandatory item (score already multiplied by 7).
        var scoreStep = item.IsGroupedDaily
            ? new ExplanationStep(
                "Score",
                "Score de la tâche",
                $"{item.Label} (7 occurrences quotidiennes regroupées) : difficulté {item.ManualDifficultyScore:0.##} × priorité {item.PriorityMultiplier:0.##} × durée {item.DurationFactor:0.##} par occurrence × 7 = {item.Score:0.##} pts au total.")
            : new ExplanationStep(
                "Score",
                "Score de la tâche",
                $"{item.Label} : difficulté {item.ManualDifficultyScore:0.##} × priorité {item.PriorityMultiplier:0.##} × durée {item.DurationFactor:0.##} = {item.Score:0.##} pts.");

        // Step 2: explain why the participant chosen during the LPT construction phase was picked (lowest
        // projected normalized load at that point in time), reusing the candidate comparison captured then.
        var loadStep = new ExplanationStep(
            "Load",
            "Comparaison des charges",
            $"{participantNames[decision.SelectedParticipantId]} avait la charge normalisée projetée la plus faible ({decision.Candidates.First(c => c.Selected).Value.GetValueOrDefault():0.##}) parmi les participants éligibles.",
            decision.Candidates);

        // Step 3: explain the BR-054 anti-repetition penalty applied to whoever ends up as the FINAL owner
        // (which may differ from the construction-phase owner if step 5 below moved it during local search).
        var occurrences = item.RecentOccurrences(finalOwnerId);
        var penalty = occurrences * settings.RepetitionPenaltyWeight * item.Score;
        var penaltyStep = occurrences > 0
            ? new ExplanationStep(
                "Penalty",
                "Pénalité anti-répétition",
                $"{participantNames[finalOwnerId]} a réalisé cette tâche {occurrences} fois durant la fenêtre d'équité récente → pénalité de {penalty:0.##} pts ajoutée à sa charge projetée (BR-054).")
            : new ExplanationStep(
                "Penalty",
                "Pénalité anti-répétition",
                $"Aucune pénalité : {participantNames[finalOwnerId]} n'a pas réalisé cette tâche récemment.");

        var steps = new List<ExplanationStep> { scoreStep, loadStep, penaltyStep };

        // Step 4: if the local search phase moved this item away from its construction-phase owner, add a
        // final step so the explanation trace doesn't silently contradict itself (step 2 names one owner,
        // the actual assignment shows another) — this makes the optimization pass visible to the user.
        if (finalOwnerId != constructionOwnerId)
        {
            steps.Add(new ExplanationStep(
                "LocalSearch",
                "Réajustement par optimisation",
                $"Cette tâche a été réattribuée de {participantNames[constructionOwnerId]} à {participantNames[finalOwnerId]} pendant la phase de recherche locale, afin de mieux équilibrer la charge globale de la semaine (BR-048)."));
        }

        return steps;
    }

    /// <summary>Groups a non-distributed mandatory daily task's 7 instances into a single item so it always moves as one block.</summary>
    private static List<AllocationItem> BuildItems(AssignmentContext context, IReadOnlyList<User> participants)
    {
        var settings = context.Settings;
        var items = new List<AllocationItem>();
        var dailyMandatoryGroups = new Dictionary<Guid, List<TaskInstance>>();

        // Step 1: walk every instance once, sorting each into either its own item or a per-template
        // "daily mandatory, not distributed" bucket to be merged afterwards.
        foreach (var instance in context.TaskInstances)
        {
            if (!context.TaskTemplatesById.TryGetValue(instance.TaskTemplateId, out var template))
            {
                // Orphaned instance (its template was deleted/not loaded): skip, nothing to score it with.
                continue;
            }

            if (template.FrequencyType == FrequencyType.DailyMandatory && !template.DistributeDailyMandatory)
            {
                // Collect this instance into its template's group instead of creating a standalone item;
                // the group is turned into one combined AllocationItem after the loop (step 2).
                if (!dailyMandatoryGroups.TryGetValue(template.Id, out var group))
                {
                    group = [];
                    dailyMandatoryGroups[template.Id] = group;
                }
                group.Add(instance);
                continue;
            }

            // Regular case: one AllocationItem per instance, scored individually via BR-052.
            var breakdown = TaskScoreCalculator.CalculateBreakdown(template, settings);
            var occurrences = BuildOccurrenceMap(context, participants, template.Id);
            items.Add(new AllocationItem(instance.Id, [instance.Id], breakdown.Score, instance.PlannedDate, occurrences, template.Title, isGroupedDaily: false, breakdown));
        }

        // Step 2: turn each collected group into a single AllocationItem whose score is the per-instance
        // score multiplied by 7 (one occurrence per day) and whose InstanceIds cover the whole week, so the
        // optimizer treats "do the dishes every day" as one indivisible unit rather than 7 separate choices.
        foreach (var (templateId, instances) in dailyMandatoryGroups)
        {
            var template = context.TaskTemplatesById[templateId];
            var breakdown = TaskScoreCalculator.CalculateBreakdown(template, settings);
            var instanceIds = instances.Select(i => i.Id).OrderBy(id => id).ToList();
            var earliestDate = instances.Min(i => i.PlannedDate);
            var occurrences = BuildOccurrenceMap(context, participants, templateId);
            items.Add(new AllocationItem(instanceIds[0], instanceIds, breakdown.Score * instances.Count, earliestDate, occurrences, template.Title, isGroupedDaily: true, breakdown));
        }

        // Step 3: sort by earliest planned date so both Construct and Improve iterate items in a stable,
        // reproducible order (important for deterministic tie-breaking and for the LPT heuristic's secondary sort key).
        return items.OrderBy(item => item.EarliestPlannedDate).ThenBy(item => item.Id).ToList();
    }

    /// <summary>How many times each participant recently did this task template, used to compute the anti-repetition penalty (BR-054).</summary>
    private static Dictionary<Guid, int> BuildOccurrenceMap(AssignmentContext context, IReadOnlyList<User> participants, Guid taskTemplateId) =>
        participants.ToDictionary(
            p => p.Id,
            p => context.HistoryByUserId.TryGetValue(p.Id, out var history)
                && history.OccurrencesByTaskTemplate.TryGetValue(taskTemplateId, out var count)
                ? count
                : 0);

    /// <summary>Largest-first (LPT) construction: the heaviest items are placed while participants are still close to even.</summary>
    private static (Dictionary<Guid, Guid> Assignment, Dictionary<Guid, ConstructionDecision> Decisions) Construct(
        IReadOnlyList<AllocationItem> items,
        IReadOnlyList<User> participants,
        IReadOnlyDictionary<Guid, decimal> baseline,
        IReadOnlyDictionary<Guid, int> baselineCount,
        IReadOnlyDictionary<Guid, decimal> target,
        AssignmentEngineSettings settings)
    {
        var assignment = new Dictionary<Guid, Guid>();
        var decisions = new Dictionary<Guid, ConstructionDecision>();
        // `load`/`count` start at each participant's historical baseline and accumulate as items are placed,
        // so this construction phase "continues" the equity window instead of starting fresh at zero.
        var load = new Dictionary<Guid, decimal>(baseline);
        var count = new Dictionary<Guid, int>(baselineCount);
        var participantNames = participants.ToDictionary(p => p.Id, p => p.DisplayName);

        // Process items heaviest-score-first (LPT = Longest Processing Time first, a classic bin-packing
        // heuristic): placing the biggest chunks of work while every participant is still near their
        // baseline leaves more "room" to fine-tune the smaller, lighter items afterwards.
        foreach (var item in items.OrderByDescending(i => i.Score).ThenBy(i => i.EarliestPlannedDate).ThenBy(i => i.Id))
        {
            // For each candidate participant, compute what their normalized load WOULD become if they took
            // this item (current load + item's score adjusted for that participant's own repetition penalty,
            // divided by their availability target) — this is a "what if" projection, not a mutation yet.
            var projectedLoad = participants.ToDictionary(p => p.Id, p => (load[p.Id] + item.AdjustedContribution(p.Id, settings)) / target[p.Id]);
            // Pick whoever would end up least-loaded; ties broken by fewer tasks so far, then by fewer recent
            // occurrences of this exact template (BR-054 preference), then by Id for full determinism.
            var candidate = participants
                .OrderBy(p => projectedLoad[p.Id])
                .ThenBy(p => count[p.Id])
                .ThenBy(p => item.RecentOccurrences(p.Id))
                .ThenBy(p => p.Id)
                .First();

            // Capture the full candidate comparison (for the BR-033 explanation trace) before committing.
            var minLoad = projectedLoad[candidate.Id];
            var candidateComparisons = participants
                .OrderBy(p => projectedLoad[p.Id])
                .Select(p => new ExplanationCandidate(
                    p.Id,
                    participantNames[p.Id],
                    projectedLoad[p.Id],
                    p.Id == candidate.Id,
                    p.Id == candidate.Id
                        ? null
                        : projectedLoad[p.Id] > minLoad
                            ? $"charge normalisée projetée plus élevée ({projectedLoad[p.Id]:0.##})"
                            : "non retenu(e) sur ce départage"))
                .ToList();

            // Commit: record the decision, then update the running totals so the NEXT item's projection
            // reflects this one having already been placed.
            assignment[item.Id] = candidate.Id;
            decisions[item.Id] = new ConstructionDecision(candidate.Id, candidateComparisons);
            load[candidate.Id] += item.Score;
            count[candidate.Id] += item.InstanceIds.Count;
        }

        return (assignment, decisions);
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
        // A single participant, or a single item, can never be rebalanced (nothing to move/swap against).
        if (items.Count < 2 || participants.Count < 2)
        {
            return;
        }

        var currentObjective = ComputeObjective(items, participants, baseline, target, settings, assignment);

        // Hill-climbing loop: each iteration searches for the single best move/swap, applies it if it
        // improves the objective, and stops as soon as no improving move exists (local optimum) or the
        // iteration budget is exhausted (safety net against pathological inputs / infinite loops).
        for (var iteration = 0; iteration < MaxLocalSearchIterations; iteration++)
        {
            Guid? bestItemA = null;
            Guid? bestItemB = null;
            var bestNewOwnerForA = Guid.Empty;
            var bestObjective = currentObjective;

            // 1) Try moving a single item to a different participant ("single move").
            // For every item, tentatively reassign it to every OTHER participant, recompute the objective
            // from scratch, and remember the best one found so far (then always revert before trying the next).
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

                    assignment[item.Id] = currentOwner; // revert the tentative move before testing the next candidate
                }
            }

            // 2) Try swapping the owners of two items between each other ("pairwise swap"),
            // which can improve the balance in ways a single move cannot (e.g. exchanging a big task for a
            // small one between two participants without changing anyone else's load).
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
                        // Swapping two items that already belong to the same person is a no-op.
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

                    assignment[itemA.Id] = ownerA; // revert the tentative swap before testing the next pair
                    assignment[itemB.Id] = ownerB;
                }
            }

            // No move or swap improved the objective this iteration: local optimum reached, stop early.
            if (bestItemA is null)
            {
                break;
            }

            // Apply whichever the best move turned out to be (a single reassignment, or a two-item swap).
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

    /// <summary>Sum of squared deviations from the mean normalized load (population variance, unnormalized); lower is a more balanced allocation.</summary>
    /// <summary>Sum of squared deviations from the mean normalized load (population variance, unnormalized); lower is a more balanced allocation.</summary>
    private static decimal ComputeObjective(
        IReadOnlyList<AllocationItem> items,
        IReadOnlyList<User> participants,
        IReadOnlyDictionary<Guid, decimal> baseline,
        IReadOnlyDictionary<Guid, decimal> target,
        AssignmentEngineSettings settings,
        IReadOnlyDictionary<Guid, Guid> assignment)
    {
        // Start every participant at their normalized historical baseline (same idea as in Construct),
        // then add each currently-assigned item's adjusted (penalty-inclusive) contribution to its owner.
        var normalizedLoad = participants.ToDictionary(p => p.Id, p => baseline[p.Id] / target[p.Id]);
        foreach (var item in items)
        {
            var owner = assignment[item.Id];
            normalizedLoad[owner] += item.AdjustedContribution(owner, settings) / target[owner];
        }

        // Sum of squared deviations from the mean: this is minimized (not maximized) by Improve, so a
        // perfectly even distribution across participants scores 0, and any imbalance scores > 0.
        var mean = normalizedLoad.Values.Average();
        return normalizedLoad.Values.Sum(value => (value - mean) * (value - mean));
    }

    /// <summary>A unit of work for the optimizer: one instance, or all 7 instances of a locked mandatory daily task.</summary>
    private sealed class AllocationItem(
        Guid id,
        IReadOnlyList<Guid> instanceIds,
        decimal score,
        DateOnly earliestPlannedDate,
        IReadOnlyDictionary<Guid, int> recentOccurrencesByParticipant,
        string label,
        bool isGroupedDaily,
        TaskScoreBreakdown breakdown)
    {
        public Guid Id { get; } = id;

        public IReadOnlyList<Guid> InstanceIds { get; } = instanceIds;

        public decimal Score { get; } = score;

        public DateOnly EarliestPlannedDate { get; } = earliestPlannedDate;

        public string Label { get; } = label;

        public bool IsGroupedDaily { get; } = isGroupedDaily;

        public decimal ManualDifficultyScore { get; } = breakdown.ManualDifficultyScore;

        public decimal PriorityMultiplier { get; } = breakdown.PriorityMultiplier;

        public decimal DurationFactor { get; } = breakdown.DurationFactor;

        public int RecentOccurrences(Guid participantId) =>
            recentOccurrencesByParticipant.TryGetValue(participantId, out var count) ? count : 0;

        /// <summary>Score plus the anti-repetition penalty (BR-054) for this specific participant.</summary>
        public decimal AdjustedContribution(Guid participantId, AssignmentEngineSettings settings) =>
            Score + RecentOccurrences(participantId) * settings.RepetitionPenaltyWeight * Score;
    }
}

/// <summary>The candidates compared, and the winner, when the LPT construction placed one allocation item.</summary>
public sealed record ConstructionDecision(Guid SelectedParticipantId, IReadOnlyList<ExplanationCandidate> Candidates);

