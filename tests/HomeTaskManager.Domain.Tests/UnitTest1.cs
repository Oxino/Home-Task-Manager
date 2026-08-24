using FluentAssertions;
using HomeTaskManager.Domain.Entities;
using HomeTaskManager.Domain.Enums;
using HomeTaskManager.Domain.Exceptions;
using HomeTaskManager.Domain.ValueObjects;

namespace HomeTaskManager.Domain.Tests;

public class DomainTests
{
    [Fact]
    public void User_defaults_to_active_and_fully_available()
    {
        var user = new User(" Julie ");

        user.DisplayName.Should().Be("Julie");
        user.IsActive.Should().BeTrue();
        user.AvailabilityPercentage.Should().Be(100);
        user.IsAdmin.Should().BeFalse();
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public void User_rejects_availability_outside_valid_range(int availability)
    {
        var action = () => new User("Julie", availabilityPercentage: availability);

        action.Should().Throw<DomainValidationException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public void Task_template_rejects_difficulty_outside_valid_range(int difficulty)
    {
        var action = () => new TaskTemplate("Aspirateur", Guid.NewGuid(), difficulty, FrequencyType.Weekly);

        action.Should().Throw<DomainValidationException>();
    }

    [Fact]
    public void Task_template_defaults_to_normal_priority_and_active()
    {
        var task = new TaskTemplate("Aspirateur", Guid.NewGuid(), 40, FrequencyType.Weekly);

        task.Priority.Should().Be(TaskPriority.Normal);
        task.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Week_key_uses_iso_week_rules()
    {
        var week = new WeekKey(2026, 1);

        week.ToString().Should().Be("2026-W01");
        week.StartDate.Should().Be(new DateOnly(2025, 12, 29));
    }

    [Fact]
    public void Week_key_rejects_week_53_when_year_has_only_52_weeks()
    {
        var action = () => new WeekKey(2025, 53);

        action.Should().Throw<DomainValidationException>();
    }

    [Fact]
    public void Week_key_next_rolls_over_to_the_following_iso_year()
    {
        var lastWeekOf2026 = new WeekKey(2026, 53);

        var next = lastWeekOf2026.Next();

        next.Year.Should().Be(2027);
        next.WeekNumber.Should().Be(1);
    }

    [Fact]
    public void Task_instance_starts_pending_and_can_be_completed_during_active_week()
    {
        var week = new Week(new WeekKey(2026, 1));
        var instance = new TaskInstance(Guid.NewGuid(), Guid.NewGuid(), week, Guid.NewGuid(), week.StartDate, week.EndDate);

        instance.IsCompleted.Should().BeFalse();
        instance.Status.Should().Be(TaskInstanceStatus.Pending);

        instance.SetCompletion(true, week);

        instance.IsCompleted.Should().BeTrue();
        instance.Status.Should().Be(TaskInstanceStatus.Completed);
    }

    [Fact]
    public void Task_instance_completion_is_locked_when_week_is_closed()
    {
        var week = new Week(new WeekKey(2026, 1));
        var instance = new TaskInstance(Guid.NewGuid(), Guid.NewGuid(), week, Guid.NewGuid(), week.StartDate, week.EndDate);
        week.Close();

        var action = () => instance.SetCompletion(true, week);

        action.Should().Throw<DomainValidationException>();
    }
}
