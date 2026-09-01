using HomeTaskManager.Api.Contracts.Weeks;
using HomeTaskManager.Application.Exceptions;
using HomeTaskManager.Application.UseCases.Weeks;
using HomeTaskManager.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using HomeTaskManager.Api;

namespace HomeTaskManager.Api.Controllers;

[ApiController]
[Route("api/weeks")]
public class WeeksController(
    IGenerateWeekInstancesUseCase generateWeekInstancesUseCase,
    IGetWeekInstancesUseCase getWeekInstancesUseCase,
    ICloseWeekUseCase closeWeekUseCase,
    IRecoverWeekContinuationUseCase recoverWeekContinuationUseCase) : ControllerBase
{
    [HttpPost("generate")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<GenerateWeekInstancesResult>> GenerateInstances(
        GenerateWeekInstancesRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var command = new GenerateWeekInstancesCommand(request.Year, request.WeekNumber, User.GetCurrentUserId());
            var result = await generateWeekInstancesUseCase.ExecuteAsync(command, cancellationToken);
            return Ok(result);
        }
        catch (DomainValidationException exception)
        {
            return BadRequest(new { error = exception.Message });
        }
    }

    [HttpGet("{year:int}/{weekNumber:int}/instances")]
    [Authorize]
    public async Task<ActionResult<GetWeekInstancesResult>> GetInstances(
        int year,
        int weekNumber,
        [FromQuery] bool includeAllParticipants,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await getWeekInstancesUseCase.ExecuteAsync(year, weekNumber, cancellationToken);
            // Default scope is always "my instances", even for admins; the multi-participant view is opt-in and admin-only.
            var canSeeAllParticipants = includeAllParticipants && User.IsInRole("Admin");
            if (!canSeeAllParticipants)
            {
                var currentUserId = User.GetCurrentUserId();
                result = result with { Instances = result.Instances.Where(instance => instance.AssignedUserId == currentUserId).ToList() };
            }
            return Ok(result);
        }
        catch (DomainValidationException exception)
        {
            return NotFound(new { error = exception.Message });
        }
    }

    [HttpPost("{year:int}/{weekNumber:int}/close")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<CloseWeekResult>> Close(
        int year,
        int weekNumber,
        CloseWeekRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var command = new CloseWeekCommand(year, weekNumber, User.GetCurrentUserId(), request.CancelIncompleteTasks);
            var result = await closeWeekUseCase.ExecuteAsync(command, cancellationToken);
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

    [HttpPost("{year:int}/{weekNumber:int}/recover-continuation")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<WeekDto>> RecoverContinuation(
        int year,
        int weekNumber,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await recoverWeekContinuationUseCase.ExecuteAsync(
                new RecoverWeekContinuationCommand(year, weekNumber, User.GetCurrentUserId()),
                cancellationToken));
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
}
