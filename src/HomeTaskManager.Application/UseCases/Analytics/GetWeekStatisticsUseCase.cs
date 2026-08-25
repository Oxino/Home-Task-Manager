using HomeTaskManager.Application.Abstractions.Repositories;
using HomeTaskManager.Domain.Entities;

namespace HomeTaskManager.Application.UseCases.Analytics;

public interface IGetWeekStatisticsUseCase
{
    Task<WeekStatisticsDto> ExecuteAsync(Guid weekId, CancellationToken cancellationToken = default);
}

public sealed record WeekStatisticsDto(Guid WeekId, List<ParticipantLoadDto> ParticipantLoads);

public sealed record ParticipantLoadDto(Guid UserId, string UserName, int TotalTasks, int CompletedTasks, int OpenTasks, int TotalPoints, double EstimatedHours);

public sealed class GetWeekStatisticsUseCase(
    IWeekRepository weekRepository,
    ITaskInstanceRepository taskInstanceRepository,
    ITaskTemplateRepository taskTemplateRepository,
    IUserRepository userRepository) : IGetWeekStatisticsUseCase
{
    public async Task<WeekStatisticsDto> ExecuteAsync(Guid weekId, CancellationToken cancellationToken = default)
    {
        var week = await weekRepository.GetByIdAsync(weekId, cancellationToken);
        if (week == null) throw new ArgumentException("Week not found", nameof(weekId));

        var instances = await taskInstanceRepository.GetByWeekAsync(weekId, cancellationToken);
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
            var hours = ((template.EstimatedDurationMinutes ?? 0) / 60d);
            participantLoads[userId] = load with
            {
                TotalTasks = load.TotalTasks + 1,
                CompletedTasks = load.CompletedTasks + (instance.IsCompleted ? 1 : 0),
                OpenTasks = load.OpenTasks + (instance.IsCompleted ? 0 : 1),
                TotalPoints = load.TotalPoints + points,
                EstimatedHours = load.EstimatedHours + hours
            };
        }

        return new WeekStatisticsDto(weekId, participantLoads.Values.ToList());
    }
}
