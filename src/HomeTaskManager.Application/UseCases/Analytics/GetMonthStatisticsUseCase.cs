using HomeTaskManager.Application.Abstractions.Repositories;

namespace HomeTaskManager.Application.UseCases.Analytics;

/// <summary>Aggregates task load (count/points/hours) per participant for a specific calendar month.</summary>
public interface IGetMonthStatisticsUseCase
{
    Task<MonthStatisticsDto> ExecuteAsync(int year, int month, CancellationToken cancellationToken = default);
}

public sealed record MonthStatisticsDto(int Year, int Month, List<ParticipantLoadDto> ParticipantLoads);

public sealed class GetMonthStatisticsUseCase(
    ITaskInstanceRepository taskInstanceRepository,
    ITaskTemplateRepository taskTemplateRepository,
    IUserRepository userRepository) : IGetMonthStatisticsUseCase
{
    public async Task<MonthStatisticsDto> ExecuteAsync(int year, int month, CancellationToken cancellationToken = default)
    {
        if (month is < 1 or > 12) throw new ArgumentOutOfRangeException(nameof(month));

        var firstDay = new DateOnly(year, month, 1);
        var lastDay = firstDay.AddMonths(1).AddDays(-1);

        // Repository only supports "since a date", so filter the upper bound (end of month) in memory.
        var instances = (await taskInstanceRepository.GetAssignedSinceAsync(firstDay, cancellationToken))
            .Where(instance => instance.PlannedDate <= lastDay)
            .ToList();
        var templates = await taskTemplateRepository.GetByIdsAsync(instances.Select(i => i.TaskTemplateId).Distinct(), cancellationToken);
        var users = await userRepository.GetAllAsync(cancellationToken);

        var templateById = templates.ToDictionary(t => t.Id);
        var userById = users.ToDictionary(u => u.Id);

        var participantLoads = new Dictionary<Guid, ParticipantLoadDto>();
        foreach (var instance in instances)
        {
            if (instance.AssignedUserId is not Guid userId) continue;
            if (!userById.TryGetValue(userId, out var user)) continue;
            if (!templateById.TryGetValue(instance.TaskTemplateId, out var template)) continue;

            if (!participantLoads.ContainsKey(userId))
                participantLoads[userId] = new ParticipantLoadDto(userId, user.DisplayName, 0, 0, 0, 0, 0);

            var load = participantLoads[userId];
            var points = template.ManualDifficultyScore;
            var hours = (template.EstimatedDurationMinutes ?? 0) / 60d;
            participantLoads[userId] = load with
            {
                TotalTasks = load.TotalTasks + 1,
                CompletedTasks = load.CompletedTasks + (instance.IsCompleted ? 1 : 0),
                OpenTasks = load.OpenTasks + (instance.IsCompleted ? 0 : 1),
                TotalPoints = load.TotalPoints + points,
                EstimatedHours = load.EstimatedHours + hours
            };
        }

        return new MonthStatisticsDto(year, month, participantLoads.Values.ToList());
    }
}
