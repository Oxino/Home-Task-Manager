using HomeTaskManager.Domain.Enums;

namespace HomeTaskManager.Application.UseCases.Settings;

public interface IGetAssignmentEngineSettingsUseCase
{
    Task<AssignmentEngineSettingsDto> ExecuteAsync(CancellationToken cancellationToken = default);
}

public sealed class GetAssignmentEngineSettingsUseCase(IAssignmentEngineSettingsProvider settingsProvider) : IGetAssignmentEngineSettingsUseCase
{
    public async Task<AssignmentEngineSettingsDto> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var settings = await settingsProvider.GetAsync(cancellationToken);

        return new AssignmentEngineSettingsDto(
            settings.PriorityMultipliers[TaskPriority.Low],
            settings.PriorityMultipliers[TaskPriority.Normal],
            settings.PriorityMultipliers[TaskPriority.Critical],
            settings.ReferenceDurationMinutes,
            settings.EquityWindowWeeks,
            settings.RepetitionPenaltyWeight);
    }
}
