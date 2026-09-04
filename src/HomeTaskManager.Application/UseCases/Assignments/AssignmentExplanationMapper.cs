using System.Text.Json;
using HomeTaskManager.Domain.Entities;

namespace HomeTaskManager.Application.UseCases.Assignments;

/// <summary>Maps the persisted <see cref="AssignmentExplanation"/> entity (steps stored as JSON) to its API DTO.</summary>
public static class AssignmentExplanationMapper
{
    public static AssignmentExplanationDto ToDto(AssignmentExplanation explanation)
    {
        // Steps are stored as raw JSON on the entity to avoid a separate table for a variable-shaped decision trace.
        var steps = JsonSerializer.Deserialize<List<ExplanationStepDto>>(explanation.StepsJson) ?? [];
        return new AssignmentExplanationDto(
            explanation.TaskInstanceId,
            explanation.AssignedUserId,
            explanation.EngineName,
            explanation.SummaryText,
            explanation.GeneratedAt,
            steps);
    }
}
