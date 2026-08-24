using FluentAssertions;
using HomeTaskManager.Domain.Engine;
using HomeTaskManager.Domain.Entities;
using HomeTaskManager.Domain.Enums;
using HomeTaskManager.Domain.ValueObjects;

namespace HomeTaskManager.Domain.Tests;

public class IntelligentV1StrategyTests
{
    [Fact]
    public void Prefers_the_participant_with_fewer_recent_occurrences_of_the_task()
    {
        // Mirrors the ASSIGNMENT_ENGINE.md worked example: equal charge, Julie did the task 4 times, Nicolas 0.
        var week = new Week(new WeekKey(2026, 10));
        var julie = new User("Julie");
        var nicolas = new User("Nicolas");
        var template = new TaskTemplate("Aspirateur", julie.Id, 40, FrequencyType.Weekly, TaskPriority.Normal, estimatedDurationMinutes: 45);
        var instance = new TaskInstance(template.Id, null, week, null, week.StartDate, week.EndDate);

        var history = new Dictionary<Guid, ParticipantHistory>
        {
            [julie.Id] = new(julie.Id, TotalScore: 0m, TotalAssignedCount: 0, new Dictionary<Guid, int> { [template.Id] = 4 }),
            [nicolas.Id] = new(nicolas.Id, TotalScore: 0m, TotalAssignedCount: 0, new Dictionary<Guid, int>()),
        };
        var context = new AssignmentContext(
            week, [julie, nicolas], [instance], new Dictionary<Guid, TaskTemplate> { [template.Id] = template },
            history, AssignmentEngineSettings.Default);

        var result = new IntelligentV1Strategy().Assign(context);

        result.Assignments.Should().ContainSingle().Which.AssignedUserId.Should().Be(nicolas.Id);
    }

    [Fact]
    public void Excludes_participants_with_zero_availability()
    {
        var week = new Week(new WeekKey(2026, 10));
        var available = new User("Julie", availabilityPercentage: 100);
        var unavailable = new User("Nicolas", availabilityPercentage: 0);
        var template = new TaskTemplate("Aspirateur", available.Id, 40, FrequencyType.Weekly);
        var instance = new TaskInstance(template.Id, null, week, null, week.StartDate, week.EndDate);
        var context = new AssignmentContext(
            week, [available, unavailable], [instance], new Dictionary<Guid, TaskTemplate> { [template.Id] = template },
            new Dictionary<Guid, ParticipantHistory>(), AssignmentEngineSettings.Default);

        var result = new IntelligentV1Strategy().Assign(context);

        result.Assignments.Should().ContainSingle().Which.AssignedUserId.Should().Be(available.Id);
    }

    [Fact]
    public void Balances_load_across_multiple_tasks_in_the_same_run()
    {
        var week = new Week(new WeekKey(2026, 10));
        var julie = new User("Julie");
        var nicolas = new User("Nicolas");
        var template = new TaskTemplate("Vaisselle", julie.Id, 20, FrequencyType.DailyMandatory);
        var instances = Enumerable.Range(0, 4)
            .Select(day => new TaskInstance(template.Id, null, week, null, week.StartDate.AddDays(day), week.StartDate.AddDays(day)))
            .ToList();
        var context = new AssignmentContext(
            week, [julie, nicolas], instances, new Dictionary<Guid, TaskTemplate> { [template.Id] = template },
            new Dictionary<Guid, ParticipantHistory>(), AssignmentEngineSettings.Default);

        var result = new IntelligentV1Strategy().Assign(context);

        result.Assignments.GroupBy(a => a.AssignedUserId).Should().HaveCount(2)
            .And.OnlyContain(group => group.Count() == 2);
    }
}
