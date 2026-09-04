using System.Globalization;
using HomeTaskManager.Application.Abstractions.Repositories;
using HomeTaskManager.Domain.Engine;
using HomeTaskManager.Domain.Enums;

namespace HomeTaskManager.Application.UseCases.Settings;

public interface IAssignmentEngineSettingsProvider
{
    Task<AssignmentEngineSettings> GetAsync(CancellationToken cancellationToken = default);
}

/// <summary>Reads BR-058 engine parameters from SystemSetting, falling back to documented defaults.</summary>
public sealed class AssignmentEngineSettingsProvider(ISystemSettingRepository systemSettingRepository) : IAssignmentEngineSettingsProvider
{
    public async Task<AssignmentEngineSettings> GetAsync(CancellationToken cancellationToken = default)
    {
        var defaults = AssignmentEngineSettings.Default;
        var byKey = (await systemSettingRepository.GetAllAsync(cancellationToken)).ToDictionary(s => s.Key, s => s.Value);

        // SystemSetting stores everything as free-form strings; parse defensively and fall back to the
        // documented default whenever a key is missing or holds an unparsable value.
        decimal ReadDecimal(string key, decimal fallback) =>
            byKey.TryGetValue(key, out var raw) && decimal.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed)
                ? parsed
                : fallback;

        int ReadInt(string key, int fallback) =>
            byKey.TryGetValue(key, out var raw) && int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
                ? parsed
                : fallback;

        return new AssignmentEngineSettings(
            new Dictionary<TaskPriority, decimal>
            {
                [TaskPriority.Low] = ReadDecimal(AssignmentEngineSettingKeys.LowPriorityMultiplier, defaults.PriorityMultipliers[TaskPriority.Low]),
                [TaskPriority.Normal] = ReadDecimal(AssignmentEngineSettingKeys.NormalPriorityMultiplier, defaults.PriorityMultipliers[TaskPriority.Normal]),
                [TaskPriority.Critical] = ReadDecimal(AssignmentEngineSettingKeys.CriticalPriorityMultiplier, defaults.PriorityMultipliers[TaskPriority.Critical]),
            },
            ReadInt(AssignmentEngineSettingKeys.ReferenceDurationMinutes, defaults.ReferenceDurationMinutes),
            ReadInt(AssignmentEngineSettingKeys.EquityWindowWeeks, defaults.EquityWindowWeeks),
            ReadDecimal(AssignmentEngineSettingKeys.RepetitionPenaltyWeight, defaults.RepetitionPenaltyWeight),
            byKey.TryGetValue(AssignmentEngineSettingKeys.StrategyName, out var strategyName) ? strategyName : defaults.StrategyName);
    }
}
