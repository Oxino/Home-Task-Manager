using FluentAssertions;
using HomeTaskManager.Domain.Engine;
using HomeTaskManager.Domain.Entities;
using HomeTaskManager.Domain.Enums;
using HomeTaskManager.Domain.ValueObjects;

namespace HomeTaskManager.Domain.Tests;

public sealed class BalancedStrategiesTests
{
    [Fact]
    public void Random_strategy_keeps_task_counts_balanced()
    {
        var context = CreateContext([40, 10, 20, 30]);
        var result = new RandomStrategy(new Random(4)).Assign(context);

        result.Assignments.GroupBy(assignment => assignment.AssignedUserId).Select(group => group.Count())
            .Should().BeEquivalentTo([2, 2]);
    }

    [Fact]
    public void Balanced_points_prioritizes_equal_score_loads()
    {
        var context = CreateContext([60, 40, 20, 20]);
        var result = new BalancedPointsStrategy().Assign(context);

        var loads = result.Assignments.GroupBy(assignment => assignment.AssignedUserId)
            .ToDictionary(group => group.Key, group => group.Sum(assignment => context.TaskTemplatesById[context.TaskInstances.Single(instance => instance.Id == assignment.TaskInstanceId).TaskTemplateId].ManualDifficultyScore));
            loads.Values.Max().Should().BeLessThanOrEqualTo(loads.Values.Min() + 20);
    }

    [Fact]
    public void Balanced_time_assigns_each_task_once()
    {
        var context = CreateContext([30, 30, 30, 30]);
        var result = new BalancedTimeStrategy().Assign(context);

        result.Assignments.Should().HaveCount(context.TaskInstances.Count);
        result.Assignments.Select(assignment => assignment.TaskInstanceId).Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void Balanced_time_alternates_the_winner_on_strict_ties()
    {
        var context = CreateContext([30, 30, 30, 30]);
        var result = new BalancedTimeStrategy().Assign(context);

        var owners = result.Assignments.Select(assignment => assignment.AssignedUserId).ToList();
        owners.Distinct().Should().HaveCount(2, "an unbroken run of strict ties should not always favor the same participant");
        result.Assignments.GroupBy(assignment => assignment.AssignedUserId).Select(group => group.Count())
            .Should().BeEquivalentTo([2, 2]);
    }

    [Fact]
    public void Balanced_points_produces_a_score_and_load_comparison_step()
    {
        var context = CreateContext([60, 40, 20, 20]);
        var result = new BalancedPointsStrategy().Assign(context);

        result.Assignments.Should().OnlyContain(assignment => assignment.ExplanationSteps != null && assignment.ExplanationSteps.Any(step => step.Kind == "Score"));
        result.Assignments.Should().OnlyContain(assignment => assignment.ExplanationSteps!.Any(step => step.Kind == "Load" && step.Candidates != null && step.Candidates.Count == 2));
    }

    private static AssignmentContext CreateContext(IReadOnlyList<int> scores)
    {
        var week = new Week(new WeekKey(2026, 1));
        var participants = new List<User> { new("Julie"), new("Nicolas") };
        var templates = scores.Select((score, index) => new TaskTemplate($"Task-{index}", participants[0].Id, score, FrequencyType.Weekly, estimatedDurationMinutes: score)).ToList();
        var instances = templates.Select(template => new TaskInstance(template.Id, null, week, null, week.StartDate, week.EndDate)).ToList();
        return new AssignmentContext(week, participants, instances, templates.ToDictionary(template => template.Id), new Dictionary<Guid, ParticipantHistory>(), AssignmentEngineSettings.Default);
    }
}