using HomeTaskManager.Api.Contracts.Assignments;
using HomeTaskManager.Application.Exceptions;
using HomeTaskManager.Application.UseCases.Assignments;
using HomeTaskManager.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using HomeTaskManager.Api;
using HomeTaskManager.Application.Abstractions.Repositories;
using HomeTaskManager.Application.UseCases.Weeks;

namespace HomeTaskManager.Api.Controllers;

/// <summary>Admin-only: triggers/re-triggers the assignment engine for a week and lists its past runs.</summary>
[ApiController]
[Route("api/assignments")]
public class AssignmentsController(IRunAssignmentEngineUseCase runAssignmentEngineUseCase, IWeekRepository weekRepository, IAssignmentRunRepository assignmentRunRepository) : ControllerBase
{
    [HttpPost("run")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<RunAssignmentEngineResult>> Run(RunAssignmentEngineRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var command = new RunAssignmentEngineCommand(request.Year, request.WeekNumber, User.GetCurrentUserId());
            var result = await runAssignmentEngineUseCase.ExecuteAsync(command, cancellationToken);
            return Ok(result);
        }
        catch (ForbiddenOperationException exception)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { error = exception.Message });
        }
        catch (DomainValidationException exception)
        {
            return BadRequest(new { error = exception.Message });
        }
    }

    [HttpGet("runs")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<IReadOnlyList<AssignmentRunDto>>> GetRuns(int year, int weekNumber, CancellationToken cancellationToken)
    {
        var week = await weekRepository.GetByKeyAsync(new Domain.ValueObjects.WeekKey(year, weekNumber), cancellationToken);
        if (week is null) return NotFound(new { error = "The requested week has not been generated yet." });
        var runs = await assignmentRunRepository.GetByWeekAsync(week.Id, cancellationToken);
        return Ok(runs.Select(AssignmentRunMapper.ToDto).ToList());
    }
}
