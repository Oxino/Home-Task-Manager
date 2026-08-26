using HomeTaskManager.Application.Abstractions;
using HomeTaskManager.Application.Abstractions.Repositories;
using HomeTaskManager.Domain.Entities;
using HomeTaskManager.Domain.Enums;
using HomeTaskManager.Domain.ValueObjects;

namespace HomeTaskManager.Application.UseCases.Weeks;

public sealed record GenerateWeekInstancesCommand(int Year, int WeekNumber);

public sealed record GenerateWeekInstancesResult(WeekDto Week, IReadOnlyList<TaskInstanceDto> CreatedInstances);

public interface IGenerateWeekInstancesUseCase
{
    Task<GenerateWeekInstancesResult> ExecuteAsync(GenerateWeekInstancesCommand command, CancellationToken cancellationToken = default);
}

/// <summary>
/// UC-GEN-01: generates the task instances of a week from active task templates.
/// Assignment (UC-ENG-01) is a separate, later step; instances start unassigned.
/// </summary>
public sealed class GenerateWeekInstancesUseCase(
    IWeekRepository weekRepository,
    ITaskTemplateRepository taskTemplateRepository,
    ITaskInstanceRepository taskInstanceRepository,
    IUnitOfWork unitOfWork) : IGenerateWeekInstancesUseCase
{
    public async Task<GenerateWeekInstancesResult> ExecuteAsync(
        GenerateWeekInstancesCommand command,
        CancellationToken cancellationToken = default)
    {
        var key = new WeekKey(command.Year, command.WeekNumber);

        var week = await weekRepository.GetByKeyAsync(key, cancellationToken);
        if (week is null)
        {
            week = new Week(key);
            await weekRepository.AddAsync(week, cancellationToken);
        }

        var activeTemplates = await taskTemplateRepository.GetActiveAsync(cancellationToken);
        var existingInstances = await taskInstanceRepository.GetByWeekAsync(week.Id, cancellationToken);
        var existingPlannedDates = existingInstances
            .Select(instance => (instance.TaskTemplateId, instance.PlannedDate))
            .ToHashSet();

        var newInstances = new List<TaskInstance>();

        foreach (var template in activeTemplates)
        {
            foreach (var plannedDate in await GetPlannedDatesAsync(template, week, cancellationToken))
            {
                if (!existingPlannedDates.Add((template.Id, plannedDate)))
                {
                    continue;
                }

                var dueDate = template.FrequencyType == FrequencyType.DailyMandatory ? plannedDate : week.EndDate;
                newInstances.Add(new TaskInstance(template.Id, null, week, null, plannedDate, dueDate));
            }
        }

        if (newInstances.Count > 0)
        {
            await taskInstanceRepository.AddRangeAsync(newInstances, cancellationToken);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new GenerateWeekInstancesResult(WeekMapper.ToDto(week), newInstances.Select(WeekMapper.ToDto).ToList());
    }

    private async Task<IReadOnlyList<DateOnly>> GetPlannedDatesAsync(
        TaskTemplate template,
        Week week,
        CancellationToken cancellationToken)
    {
        switch (template.FrequencyType)
        {
            case FrequencyType.DailyMandatory:
                return Enumerable.Range(0, 7).Select(week.StartDate.AddDays).ToList();

            case FrequencyType.DailyFlexible:
            case FrequencyType.Weekly:
                return [week.StartDate];

            case FrequencyType.WeeklyFlexible:
                return template.GetActiveWeekDays()
                    .Select(day => week.StartDate.AddDays(((int)day + 6) % 7))
                    .ToList();

            case FrequencyType.Monthly:
                // R-12: default rule, generate on the first calendar week that starts within the month.
                return week.StartDate.Day <= 7 ? [week.StartDate] : [];

            case FrequencyType.OneTime:
                var alreadyGenerated = await taskInstanceRepository.ExistsForTaskTemplateAsync(template.Id, cancellationToken);
                return alreadyGenerated ? [] : [week.StartDate];

            default:
                return [];
        }
    }
}
