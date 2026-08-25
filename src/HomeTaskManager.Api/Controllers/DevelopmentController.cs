using HomeTaskManager.Api.Contracts.Development;
using HomeTaskManager.Api;
using HomeTaskManager.Application.Abstractions.Repositories;
using HomeTaskManager.Application.UseCases.Assignments;
using HomeTaskManager.Application.UseCases.Weeks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;

namespace HomeTaskManager.Api.Controllers;

[ApiController]
[Route("api/development")]
[Authorize(Roles = "Admin")]
public sealed class DevelopmentController(
    IGenerateWeekInstancesUseCase generateWeekInstancesUseCase,
    IRunAssignmentEngineUseCase runAssignmentEngineUseCase,
    IWeekRepository weekRepository,
    IAssignmentRunRepository assignmentRunRepository) : ControllerBase
{
    [HttpPost("seed-history")]
    public async Task<ActionResult<object>> SeedHistory(SeedHistoryRequest request, CancellationToken cancellationToken)
    {
        if (request.Weeks is < 1 or > 52)
            return BadRequest(new { error = "Weeks must be between 1 and 52." });

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var currentWeek = ISOWeek.GetWeekOfYear(today.ToDateTime(TimeOnly.MinValue));
        var currentYear = ISOWeek.GetYear(today.ToDateTime(TimeOnly.MinValue));
        var generatedWeeks = new List<object>();

        for (var offset = request.Weeks - 1; offset >= 0; offset--)
        {
            var monday = DateOnly.FromDateTime(ISOWeek.ToDateTime(currentYear, currentWeek, DayOfWeek.Monday)).AddDays(-offset * 7);
            var year = ISOWeek.GetYear(monday.ToDateTime(TimeOnly.MinValue));
            var weekNumber = ISOWeek.GetWeekOfYear(monday.ToDateTime(TimeOnly.MinValue));
            var generated = await generateWeekInstancesUseCase.ExecuteAsync(new GenerateWeekInstancesCommand(year, weekNumber), cancellationToken);
            var week = await weekRepository.GetByKeyAsync(new Domain.ValueObjects.WeekKey(year, weekNumber), cancellationToken);
            var alreadyExecuted = week is not null && await assignmentRunRepository.ExistsForWeekAsync(week.Id, cancellationToken);
            if (!alreadyExecuted)
                await runAssignmentEngineUseCase.ExecuteAsync(new RunAssignmentEngineCommand(year, weekNumber, User.GetCurrentUserId()), cancellationToken);
            generatedWeeks.Add(new { year, weekNumber, createdInstances = generated.CreatedInstances.Count, executionCreated = !alreadyExecuted });
        }

        return Ok(new { weeks = generatedWeeks });
    }
}