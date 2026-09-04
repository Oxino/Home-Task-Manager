using HomeTaskManager.Domain.Entities;

namespace HomeTaskManager.Application.UseCases.Assignments;

public static class AssignmentRunMapper
{
    /// <summary>Flat 1:1 projection, no derived fields needed.</summary>
    public static AssignmentRunDto ToDto(AssignmentRun run) =>
        new(run.Id, run.WeekId, run.StrategyName, run.TriggeredByUserId, run.IsAutomatic, run.Notes, run.GeneratedAt);
}
