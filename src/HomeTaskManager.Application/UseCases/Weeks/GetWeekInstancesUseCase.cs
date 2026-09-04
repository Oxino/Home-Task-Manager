using HomeTaskManager.Application.Abstractions.Repositories;
using HomeTaskManager.Domain.Exceptions;
using HomeTaskManager.Domain.ValueObjects;

namespace HomeTaskManager.Application.UseCases.Weeks;

public sealed record GetWeekInstancesResult(WeekDto Week, IReadOnlyList<TaskInstanceDto> Instances);

/// <summary>Reads a week and its task instances; carried-over instances are enriched with their original week/due date for display.</summary>
public interface IGetWeekInstancesUseCase
{
    Task<GetWeekInstancesResult> ExecuteAsync(int year, int weekNumber, CancellationToken cancellationToken = default);
}

public sealed class GetWeekInstancesUseCase(
    IWeekRepository weekRepository,
    ITaskInstanceRepository taskInstanceRepository) : IGetWeekInstancesUseCase
{
    public async Task<GetWeekInstancesResult> ExecuteAsync(int year, int weekNumber, CancellationToken cancellationToken = default)
    {
        var key = new WeekKey(year, weekNumber);
        var week = await weekRepository.GetByKeyAsync(key, cancellationToken)
            ?? throw new DomainValidationException("The requested week has not been generated yet.");

        var instances = await taskInstanceRepository.GetByWeekAsync(week.Id, cancellationToken);

        var instanceDtos = new List<TaskInstanceDto>();
        foreach (var instance in instances)
        {
            var dto = WeekMapper.ToDto(instance);
            if (instance.ParentTaskInstanceId is Guid parentId)
            {
                // Carry-over instance (ADR-005): look up its original occurrence so the UI can show
                // "originally due in week X" instead of just the new planned/due date.
                var parent = await taskInstanceRepository.GetByIdAsync(parentId, cancellationToken);
                var parentWeek = parent is null ? null : await weekRepository.GetByIdAsync(parent.WeekId, cancellationToken);
                if (parent is not null && parentWeek is not null)
                    dto = dto with { OriginalWeekYear = parentWeek.Year, OriginalWeekNumber = parentWeek.WeekNumber, OriginalDueDate = parent.DueDate };
            }
            instanceDtos.Add(dto);
        }

        return new GetWeekInstancesResult(WeekMapper.ToDto(week), instanceDtos);
    }
}
