using HomeTaskManager.Application.Abstractions.Repositories;
using System.Globalization;

namespace HomeTaskManager.Application.UseCases.Analytics;

public interface IGetUserLoadDistributionUseCase
{
    Task<UserLoadDistributionDto> ExecuteAsync(CancellationToken cancellationToken = default);
}

public sealed record UserLoadDistributionDto(List<UserLoadDto> UserLoads);

public sealed record UserLoadDto(Guid UserId, string UserName, List<WeekLoadDto> WeeklyLoads, double AverageLoad);

public sealed record WeekLoadDto(int WeekNumber, int Year, int AssignedTasks, int CompletedTasks);

public sealed class GetUserLoadDistributionUseCase(
    ITaskInstanceRepository taskInstanceRepository,
    IUserRepository userRepository) : IGetUserLoadDistributionUseCase
{
    public async Task<UserLoadDistributionDto> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var instances = await taskInstanceRepository.GetAssignedSinceAsync(DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-140), cancellationToken);
        var users = await userRepository.GetAllAsync(cancellationToken);

        var userById = users.ToDictionary(u => u.Id);

        var userLoads = new Dictionary<Guid, Dictionary<(int, int), (int, int)>>();

        foreach (var instance in instances)
        {
            if (instance.AssignedUserId is not Guid userId) continue;
            if (!userById.ContainsKey(userId)) continue;

            if (!userLoads.ContainsKey(userId)) userLoads[userId] = new();

            var weekKey = (instance.PlannedDate.Year, ISOWeek.GetWeekOfYear(instance.PlannedDate.ToDateTime(TimeOnly.MinValue)));
            if (!userLoads[userId].ContainsKey(weekKey)) userLoads[userId][weekKey] = (0, 0);

            var (assigned, completed) = userLoads[userId][weekKey];
            userLoads[userId][weekKey] = (
                assigned + 1,
                completed + (instance.IsCompleted ? 1 : 0)
            );
        }

        var result = new List<UserLoadDto>();
        foreach (var userId in userLoads.Keys)
        {
            if (!userById.TryGetValue(userId, out var user)) continue;

            var weeklyLoads = userLoads[userId]
                .OrderBy(k => k.Key.Item1)
                .ThenBy(k => k.Key.Item2)
                .Select(kvp => new WeekLoadDto(kvp.Key.Item2, kvp.Key.Item1, kvp.Value.Item1, kvp.Value.Item2))
                .ToList();

            var avgLoad = weeklyLoads.Any() ? (weeklyLoads.Average(w => (double)w.AssignedTasks)) : 0d;
            result.Add(new UserLoadDto(userId, user.DisplayName, weeklyLoads, avgLoad));
        }

        return new UserLoadDistributionDto(result);
    }
}
