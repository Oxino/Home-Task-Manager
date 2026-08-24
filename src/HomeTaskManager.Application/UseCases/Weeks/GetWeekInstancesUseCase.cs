using HomeTaskManager.Application.Abstractions.Repositories;
using HomeTaskManager.Domain.Exceptions;
using HomeTaskManager.Domain.ValueObjects;

namespace HomeTaskManager.Application.UseCases.Weeks;

public sealed record GetWeekInstancesResult(WeekDto Week, IReadOnlyList<TaskInstanceDto> Instances);

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

        return new GetWeekInstancesResult(WeekMapper.ToDto(week), instances.Select(WeekMapper.ToDto).ToList());
    }
}
