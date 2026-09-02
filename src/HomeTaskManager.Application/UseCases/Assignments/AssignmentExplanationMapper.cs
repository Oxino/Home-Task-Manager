using System.Text.Json;
using HomeTaskManager.Domain.Entities;

namespace HomeTaskManager.Application.UseCases.Assignments;

public static class AssignmentExplanationMapper
{
    public static AssignmentExplanationDto ToDto(AssignmentExplanation explanation)
    {
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
