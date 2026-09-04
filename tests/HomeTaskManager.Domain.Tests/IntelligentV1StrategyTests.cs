using FluentAssertions;
using HomeTaskManager.Domain.Engine;
using HomeTaskManager.Domain.Entities;
using HomeTaskManager.Domain.Enums;
using HomeTaskManager.Domain.ValueObjects;

namespace HomeTaskManager.Domain.Tests;

/// <summary>Covers the INTELLIGENT_V1 assignment strategy: anti-repetition preference, and the BR-033 explanation steps it produces.</summary>
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
    public void Produces_score_load_and_penalty_explanation_steps()
    {
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

        var steps = result.Assignments.Should().ContainSingle().Subject.ExplanationSteps;
        steps.Should().NotBeNull();
        steps!.Select(step => step.Kind).Should().Contain("Score").And.Contain("Load").And.Contain("Penalty");
        var loadStep = steps!.Single(step => step.Kind == "Load");
        loadStep.Candidates.Should().HaveCount(2);
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
    public void Keeps_all_instances_of_a_daily_mandatory_task_with_one_participant_when_distribution_is_disabled()
    {
        var week = new Week(new WeekKey(2026, 10));
        var julie = new User("Julie");
        var nicolas = new User("Nicolas");
        var template = new TaskTemplate("Vaisselle", julie.Id, 20, FrequencyType.DailyMandatory, distributeDailyMandatory: false);
        var instances = Enumerable.Range(0, 4)
            .Select(day => new TaskInstance(template.Id, null, week, null, week.StartDate.AddDays(day), week.StartDate.AddDays(day)))
            .ToList();
        var context = new AssignmentContext(
            week, [julie, nicolas], instances, new Dictionary<Guid, TaskTemplate> { [template.Id] = template },
            new Dictionary<Guid, ParticipantHistory>(), AssignmentEngineSettings.Default);

        var result = new IntelligentV1Strategy().Assign(context);

        result.Assignments.Should().HaveCount(4);
        result.Assignments.Select(a => a.AssignedUserId).Distinct().Should().ContainSingle();
    }

    [Fact]
    public void Spreads_daily_mandatory_instances_across_participants_by_default()
    {
        var week = new Week(new WeekKey(2026, 10));
        var julie = new User("Julie");
        var nicolas = new User("Nicolas");
        var template = new TaskTemplate("Vaisselle", julie.Id, 20, FrequencyType.DailyMandatory);
        var history = new Dictionary<Guid, ParticipantHistory>
        {
            // Nicolas already carries a heavy load, so a distributing engine should favor Julie on every instance.
            [julie.Id] = new(julie.Id, TotalScore: 0m, TotalAssignedCount: 0, new Dictionary<Guid, int>()),
            [nicolas.Id] = new(nicolas.Id, TotalScore: 500m, TotalAssignedCount: 10, new Dictionary<Guid, int>()),
        };
        var instances = Enumerable.Range(0, 4)
            .Select(day => new TaskInstance(template.Id, null, week, null, week.StartDate.AddDays(day), week.StartDate.AddDays(day)))
            .ToList();
        var context = new AssignmentContext(
            week, [julie, nicolas], instances, new Dictionary<Guid, TaskTemplate> { [template.Id] = template },
            history, AssignmentEngineSettings.Default);

        var result = new IntelligentV1Strategy().Assign(context);

        result.Assignments.Should().HaveCount(4);
        result.Assignments.Should().OnlyContain(a => a.AssignedUserId == julie.Id);
    }

    [Fact]
    public void Global_optimization_avoids_an_all_or_nothing_week_when_history_is_close()
    {
        // Reproduces the real-world regression: a single big-score decision (Salle de bain, 45 pts) made
        // early in a chronological pass used to permanently tip the rest of a week to one participant,
        // even when both started the week with a comparable history. The construction+local-search
        // optimizer must keep both participants in the split instead of sweeping everything to one side.
        var week = new Week(new WeekKey(2026, 10));
        var alice = new User("Alice");
        var benoit = new User("Benoit");
        var bigTemplate = new TaskTemplate("Salle de bain", alice.Id, 30, FrequencyType.Weekly, TaskPriority.Normal, estimatedDurationMinutes: 45);
        var smallTemplate = new TaskTemplate("Vaisselle", alice.Id, 20, FrequencyType.DailyMandatory, TaskPriority.Normal, estimatedDurationMinutes: 20);
        var instances = new List<TaskInstance>
        {
            new(bigTemplate.Id, null, week, null, week.StartDate, week.EndDate),
        };
        instances.AddRange(Enumerable.Range(0, 7)
            .Select(day => new TaskInstance(smallTemplate.Id, null, week, null, week.StartDate.AddDays(day), week.StartDate.AddDays(day))));

        var history = new Dictionary<Guid, ParticipantHistory>
        {
            [alice.Id] = new(alice.Id, TotalScore: 150m, TotalAssignedCount: 8, new Dictionary<Guid, int>()),
            [benoit.Id] = new(benoit.Id, TotalScore: 170m, TotalAssignedCount: 10, new Dictionary<Guid, int>()),
        };
        var templatesById = new Dictionary<Guid, TaskTemplate> { [bigTemplate.Id] = bigTemplate, [smallTemplate.Id] = smallTemplate };
        var context = new AssignmentContext(week, [alice, benoit], instances, templatesById, history, AssignmentEngineSettings.Default);

        var result = new IntelligentV1Strategy().Assign(context);

        result.Assignments.Should().HaveCount(8);
        var countByParticipant = result.Assignments
            .GroupBy(a => a.AssignedUserId)
            .ToDictionary(g => g.Key, g => g.Count());
        countByParticipant.Should().ContainKey(alice.Id).And.ContainKey(benoit.Id);
        countByParticipant[alice.Id].Should().BeGreaterThan(0);
        countByParticipant[benoit.Id].Should().BeGreaterThan(0);
    }
}
