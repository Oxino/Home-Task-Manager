using HomeTaskManager.Application.Abstractions.Repositories;

namespace HomeTaskManager.Application.UseCases.Analytics;

/// <summary>Reports each user's completion rate over the last 28 days, used for the analytics dashboard.</summary>
public interface IGetCompletionStatisticsUseCase
{
    Task<CompletionStatisticsDto> ExecuteAsync(CancellationToken cancellationToken = default);
}

public sealed record CompletionStatisticsDto(List<UserCompletionDto> UserCompletions);

public sealed record UserCompletionDto(Guid UserId, string UserName, int CompletedTasks, int TotalTasks, double CompletionPercentage);

public sealed class GetCompletionStatisticsUseCase(
    ITaskInstanceRepository taskInstanceRepository,
    IUserRepository userRepository) : IGetCompletionStatisticsUseCase
{
    public async Task<CompletionStatisticsDto> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        // Fixed 4-week rolling window (not admin-configurable) for the completion-rate widget.
        var instances = await taskInstanceRepository.GetAssignedSinceAsync(DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-28), cancellationToken);
        var users = await userRepository.GetAllAsync(cancellationToken);

        var userById = users.ToDictionary(u => u.Id);
        var completionStats = new Dictionary<Guid, (int completed, int total)>();

        foreach (var instance in instances)
        {
            if (instance.AssignedUserId is not Guid userId) continue;
            if (!completionStats.ContainsKey(userId)) completionStats[userId] = (0, 0);

            var (completed, total) = completionStats[userId];
            completionStats[userId] = (
                completed + (instance.IsCompleted ? 1 : 0),
                total + 1
            );
        }

        var dtos = new List<UserCompletionDto>();
        foreach (var userId in completionStats.Keys)
        {
            if (!userById.TryGetValue(userId, out var user)) continue;
            var (completed, total) = completionStats[userId];
            var percentage = total > 0 ? ((completed * 100d) / total) : 0d;
            dtos.Add(new UserCompletionDto(userId, user.DisplayName, completed, total, percentage));
        }

        return new CompletionStatisticsDto(dtos.OrderByDescending(d => d.CompletionPercentage).ToList());
    }
}
