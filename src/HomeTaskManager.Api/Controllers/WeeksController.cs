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
    ICloseWeekUseCase closeWeekUseCase) : ControllerBase
{
    [HttpPost("generate")]
    public async Task<ActionResult<GenerateWeekInstancesResult>> GenerateInstances(
        GenerateWeekInstancesRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var command = new GenerateWeekInstancesCommand(request.Year, request.WeekNumber);
            var result = await generateWeekInstancesUseCase.ExecuteAsync(command, cancellationToken);
            return Ok(result);
        }
        catch (DomainValidationException exception)
        {
            return BadRequest(new { error = exception.Message });
        }
    }

    [HttpGet("{year:int}/{weekNumber:int}/instances")]
    public async Task<ActionResult<GetWeekInstancesResult>> GetInstances(
        int year,
        int weekNumber,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await getWeekInstancesUseCase.ExecuteAsync(year, weekNumber, cancellationToken);
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
            var command = new CloseWeekCommand(year, weekNumber, User.GetCurrentUserId());
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
}
