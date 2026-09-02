using FluentAssertions;
using HomeTaskManager.Domain.Engine;
using HomeTaskManager.Domain.Entities;
using HomeTaskManager.Domain.Enums;
using HomeTaskManager.Domain.ValueObjects;

namespace HomeTaskManager.Domain.Tests;

public class RandomStrategyTests
{
    [Fact]
    public void Assigns_every_task_to_an_active_participant()
    {
        var week = new Week(new WeekKey(2026, 1));
        var participants = new List<User> { new("Julie"), new("Nicolas") };
        var templateId = Guid.NewGuid();
        var instance = new TaskInstance(templateId, null, week, null, week.StartDate, week.EndDate);
        var context = new AssignmentContext(
            week, participants, [instance], new Dictionary<Guid, TaskTemplate>(), new Dictionary<Guid, ParticipantHistory>(), AssignmentEngineSettings.Default);
        var strategy = new RandomStrategy(new Random(42));

        var result = strategy.Assign(context);

        result.Assignments.Should().ContainSingle();
        participants.Select(p => p.Id).Should().Contain(result.Assignments[0].AssignedUserId);
    }

    [Fact]
    public void Returns_no_assignment_when_there_is_no_active_participant()
    {
        var week = new Week(new WeekKey(2026, 1));
        var instance = new TaskInstance(Guid.NewGuid(), null, week, null, week.StartDate, week.EndDate);
        var context = new AssignmentContext(
            week, [], [instance], new Dictionary<Guid, TaskTemplate>(), new Dictionary<Guid, ParticipantHistory>(), AssignmentEngineSettings.Default);
        var strategy = new RandomStrategy();

        var result = strategy.Assign(context);

        result.Assignments.Should().BeEmpty();
    }

    [Fact]
    public void Produces_a_random_explanation_step_for_each_assignment()
    {
        var week = new Week(new WeekKey(2026, 1));
        var participants = new List<User> { new("Julie"), new("Nicolas") };
        var templateId = Guid.NewGuid();
        var instance = new TaskInstance(templateId, null, week, null, week.StartDate, week.EndDate);
        var context = new AssignmentContext(
            week, participants, [instance], new Dictionary<Guid, TaskTemplate>(), new Dictionary<Guid, ParticipantHistory>(), AssignmentEngineSettings.Default);
        var strategy = new RandomStrategy(new Random(42));

        var result = strategy.Assign(context);

        result.Assignments.Should().ContainSingle().Which.ExplanationSteps.Should().ContainSingle(step => step.Kind == "Random");
    }
}
