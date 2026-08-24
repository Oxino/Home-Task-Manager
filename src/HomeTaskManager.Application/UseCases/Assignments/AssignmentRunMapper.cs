using HomeTaskManager.Domain.Entities;

namespace HomeTaskManager.Application.UseCases.Assignments;

internal static class AssignmentRunMapper
{
    public static AssignmentRunDto ToDto(AssignmentRun run) =>
        new(run.Id, run.WeekId, run.StrategyName, run.TriggeredByUserId, run.IsAutomatic, run.Notes, run.GeneratedAt);
}
